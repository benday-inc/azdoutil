namespace Benday.AzureDevOpsUtil.Api.TfvcAssessment;

/// <summary>
/// Runs the assessment and turns what it observed into findings.
///
/// Findings state a fact and what that fact means for a conversion to Git.  They
/// do not rank, score, or recommend.
/// </summary>
public class TfvcAssessmentAnalyzer
{
    private readonly ITfvcApiClient _client;
    private readonly TfvcBranchHierarchyService _hierarchyService;
    private readonly TfvcFolderHeuristicScanner _folderScanner;
    private readonly TfvcBranchActivityService _activityService;

    public TfvcAssessmentAnalyzer(ITfvcApiClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _hierarchyService = new TfvcBranchHierarchyService();
        _folderScanner = new TfvcFolderHeuristicScanner();
        _activityService = new TfvcBranchActivityService();
    }

    /// <summary>
    /// Called before each branch is measured for activity, so a long-running
    /// scan can report progress.
    /// </summary>
    public Action<string>? ProgressCallback { get; set; }

    public int MaxScanDepth { get; set; } = TfvcFolderHeuristicScanner.DefaultMaxDepth;

    public int MaxChangesetsPerBranch { get; set; } =
        TfvcBranchActivityService.DefaultMaxChangesetsPerBranch;

    public async Task<TfvcAssessmentResult> AnalyzeAsync(
        string projectName, string scopePath, DateTime utcNow)
    {
        var scope = TfvcPath.Normalize(scopePath);

        var result = new TfvcAssessmentResult
        {
            ProjectName = projectName,
            ScopePath = scope,
            GeneratedUtc = utcNow
        };

        ReportProgress("Reading registered branches...");

        var branches = await _client.GetBranchesAsync(projectName);

        var hierarchy = _hierarchyService.Build(branches, scope);

        result.RegisteredBranchRoots = hierarchy.Roots;
        result.RegisteredBranchPaths = hierarchy.AllPaths;
        result.NestedBranches = hierarchy.NestedBranches;

        ReportProgress($"Scanning folders for unregistered branches (depth {MaxScanDepth})...");

        result.UnregisteredBranchGroups = await _folderScanner.ScanAsync(
            _client, projectName, scope, hierarchy.AllPaths, MaxScanDepth);

        result.Notes.Add(
            $"Folder scan walked {MaxScanDepth} level(s) below {scope}. " +
            "Folders deeper than that were not examined.");

        var candidates = BuildBranchCandidates(result);

        ReportProgress($"Measuring changeset activity for {candidates.Count} branch(es)...");

        result.BranchActivity = await _activityService.AnalyzeAsync(
            _client, projectName, candidates, utcNow, MaxChangesetsPerBranch);

        if (result.BranchActivity.Any(x => x.CountsAreCapped == true) == true)
        {
            result.Notes.Add(
                $"Changeset counting stopped at {MaxChangesetsPerBranch} per branch. " +
                "Counts marked with a plus sign are a floor, not an exact number.");
        }

        BuildFindings(result);

        return result;
    }

    private List<BranchCandidate> BuildBranchCandidates(TfvcAssessmentResult result)
    {
        var candidates = new List<BranchCandidate>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in result.RegisteredBranchPaths)
        {
            if (seen.Add(TfvcPath.Normalize(path)) == true)
            {
                candidates.Add(new BranchCandidate(path, true));
            }
        }

        foreach (var group in result.UnregisteredBranchGroups)
        {
            foreach (var path in group.FolderPaths)
            {
                if (seen.Add(TfvcPath.Normalize(path)) == true)
                {
                    candidates.Add(new BranchCandidate(path, false));
                }
            }
        }

        return candidates;
    }

    private void BuildFindings(TfvcAssessmentResult result)
    {
        AddBranchHierarchyFindings(result);
        AddNestedBranchFindings(result);
        AddUnregisteredBranchFindings(result);
        AddBranchActivityFindings(result);
    }

    private void AddBranchHierarchyFindings(TfvcAssessmentResult result)
    {
        if (result.RegisteredBranchPaths.Count > 0)
        {
            result.Findings.Add(new AssessmentFinding(
                FindingCategories.BranchHierarchy,
                $"{result.RegisteredBranchPaths.Count} folder(s) under {result.ScopePath} " +
                    "are registered as branches in TFVC.",
                "The Azure DevOps TFVC-to-Git import converts a single TFVC path. " +
                    "Any branch that is not chosen as the import source will not exist " +
                    "in the resulting Git repository."));

            return;
        }

        result.Findings.Add(new AssessmentFinding(
            FindingCategories.BranchHierarchy,
            $"No TFVC folders under {result.ScopePath} are registered as branches.",
            "Branch relationships, if any exist, are not recorded in TFVC metadata " +
                "and cannot be discovered through the branches API."));
    }

    private void AddNestedBranchFindings(TfvcAssessmentResult result)
    {
        foreach (var pair in result.NestedBranches)
        {
            result.Findings.Add(new AssessmentFinding(
                FindingCategories.NestedBranches,
                $"{pair.ChildPath} is a branch rooted inside the branch {pair.ParentPath}.",
                "Nested branches cannot be represented by the Azure DevOps " +
                    "TFVC-to-Git import."));
        }
    }

    private void AddUnregisteredBranchFindings(TfvcAssessmentResult result)
    {
        foreach (var group in result.UnregisteredBranchGroups)
        {
            var list = string.Join(", ", group.FolderPaths);

            result.Findings.Add(new AssessmentFinding(
                FindingCategories.UnregisteredBranches,
                "These folders appear to be branch copies but are not registered " +
                    $"as branches: {list}.",
                "Their relationship to each other is not recorded anywhere in TFVC. " +
                    "The import treats each as ordinary folders.",
                $"Parent folder: {group.ParentPath}"));
        }
    }

    private void AddBranchActivityFindings(TfvcAssessmentResult result)
    {
        if (result.BranchActivity.Count == 0)
        {
            return;
        }

        var activeCount = result.ActiveBranchCount;

        if (activeCount > 0)
        {
            result.Findings.Add(new AssessmentFinding(
                FindingCategories.BranchActivity,
                $"{activeCount} branch(es) have had changes in the last 90 days.",
                "Each active branch is in-flight work that must be accounted for " +
                    "in a migration.",
                string.Join(", ", result.BranchActivity
                    .Where(x => x.Classification == BranchActivityClassification.Active)
                    .Select(x => x.Path))));
        }

        // Two or more branches taking check-ins at the same time is what makes
        // the shape of the import matter.  This does not need merge history to
        // be true; it follows from the import creating unrelated histories.
        if (activeCount > 1)
        {
            result.Findings.Add(new AssessmentFinding(
                FindingCategories.BranchActivity,
                $"{activeCount} branches are active at the same time.",
                "The Azure DevOps import creates each branch as an unrelated Git " +
                    "history with no common ancestor. Git cannot merge branches that " +
                    "share no common ancestor. In-flight work on these branches cannot " +
                    "be merged after migration through normal means."));
        }

        var deadCount = result.DeadBranchCount;

        if (deadCount > 0)
        {
            result.Findings.Add(new AssessmentFinding(
                FindingCategories.BranchActivity,
                $"{deadCount} branch(es) have had no changes in the last 365 days.",
                "Nothing in the branch metadata records whether the contents of these " +
                    "branches exist anywhere else.",
                string.Join(", ", result.BranchActivity
                    .Where(x => x.Classification == BranchActivityClassification.Dead)
                    .Select(x => x.Path))));
        }
    }

    private void ReportProgress(string message)
    {
        ProgressCallback?.Invoke(message);
    }
}
