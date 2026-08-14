using Benday.AzureDevOpsUtil.Api.Messages;

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
    private readonly IBuildDefinitionApiClient? _buildClient;
    private readonly TfvcBranchHierarchyService _hierarchyService;
    private readonly TfvcFolderHeuristicScanner _folderScanner;
    private readonly TfvcBranchActivityService _activityService;
    private readonly BuildDefinitionWorkspaceService _buildWorkspaceService;
    private readonly TfvcContentScanner _contentScanner;
    private readonly TfvcSolutionAnalysisService _solutionService;

    /// <param name="buildClient">
    /// Optional.  When it is not supplied the build definition section is
    /// skipped and the report says so instead of going quiet.
    /// </param>
    public TfvcAssessmentAnalyzer(
        ITfvcApiClient client, IBuildDefinitionApiClient? buildClient = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _buildClient = buildClient;
        _hierarchyService = new TfvcBranchHierarchyService();
        _folderScanner = new TfvcFolderHeuristicScanner();
        _activityService = new TfvcBranchActivityService();
        _buildWorkspaceService = new BuildDefinitionWorkspaceService();
        _contentScanner = new TfvcContentScanner();
        _solutionService = new TfvcSolutionAnalysisService();
    }

    /// <summary>
    /// Called as the scan moves between sections and items, so a long-running
    /// assessment can report progress.
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

        await ScanContentAsync(result, projectName, scope);

        await ScanBuildDefinitionsAsync(result, projectName, utcNow);

        BuildFindings(result);

        return result;
    }

    /// <summary>
    /// Reads the whole tree in one recursive listing.  A failure here is
    /// recorded and the rest of the assessment carries on: on a very large tree
    /// this is a single large response, and losing the branch analysis to it
    /// would be a poor trade.
    /// </summary>
    private async Task ScanContentAsync(
        TfvcAssessmentResult result, string projectName, string scope)
    {
        ReportProgress($"Reading the contents of {scope}...");

        try
        {
            var items = await _client.GetItemsAsync(projectName, scope, TfvcRecursionLevel.Full);

            result.Content = _contentScanner.Scan(items, scope);

            result.Notes.Add(
                $"{result.Content.FileCount} file(s) were examined under {scope}. " +
                "Sizes are the current version of each file, not the size of the history.");

            // The same listing names every solution and project file, so the
            // solution analysis reuses it rather than walking the tree again.
            await ScanSolutionsAsync(result, projectName, items);
        }
        catch (Exception ex)
        {
            result.Notes.Add(
                $"The contents of {scope} could not be read: {ex.Message} " +
                "The rest of this report is unaffected.");
        }
    }

    /// <summary>
    /// Reads every solution and project file found in the listing.  Failures
    /// are recorded per file, so one unreadable project does not cost the
    /// whole section.
    /// </summary>
    private async Task ScanSolutionsAsync(
        TfvcAssessmentResult result, string projectName, IReadOnlyList<TfvcItemInfo> items)
    {
        ReportProgress("Reading solutions and project files...");

        _solutionService.ProgressCallback = ProgressCallback;

        result.Solutions = await _solutionService.AnalyzeAsync(_client, projectName, items);

        if (result.Solutions.UnreadableFiles.Count > 0)
        {
            result.Notes.Add(
                $"{result.Solutions.UnreadableFiles.Count} solution or project file(s) " +
                "could not be read: " +
                string.Join(", ", result.Solutions.UnreadableFiles.Take(10)) + ".");
        }
    }

    /// <summary>
    /// Reads the build definitions for the whole team project.  A failure here
    /// is recorded and the rest of the assessment carries on, because the build
    /// section is the part most likely to be blocked by permissions.
    /// </summary>
    private async Task ScanBuildDefinitionsAsync(
        TfvcAssessmentResult result, string projectName, DateTime utcNow)
    {
        if (_buildClient == null)
        {
            result.Notes.Add(
                "Build definitions were not examined because no build API client was supplied.");

            return;
        }

        ReportProgress("Reading build definitions...");

        _buildWorkspaceService.ProgressCallback = ProgressCallback;

        BuildDefinitionScanResult scan;

        try
        {
            scan = await _buildWorkspaceService.ScanAsync(_buildClient, projectName, utcNow);
        }
        catch (Exception ex)
        {
            result.Notes.Add(
                $"Build definitions could not be read: {ex.Message} " +
                "The rest of this report is unaffected.");

            return;
        }

        result.TfvcBuildDefinitions = scan.Definitions;
        result.MappedPathUsages = scan.MappedPathUsages;

        if (scan.UnreadableDefinitions.Count > 0)
        {
            result.Notes.Add(
                $"{scan.UnreadableDefinitions.Count} build definition(s) could not be read: " +
                string.Join(", ", scan.UnreadableDefinitions) + ".");
        }

        result.Notes.Add(
            $"Build definitions were read for the whole {projectName} team project, " +
            $"not just {result.ScopePath}. {scan.TotalDefinitionsExamined} definition(s) " +
            "were examined.");
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
        result.CommonCodeFolders = BuildCommonCodeFolders(result);

        AddContentFindings(result);
        AddSolutionFindings(result);
        AddBuildDefinitionFindings(result);
        AddSharedFolderFindings(result);
        AddCommonCodeFindings(result);
    }

    private void AddSolutionFindings(TfvcAssessmentResult result)
    {
        var solutions = result.Solutions;

        if (solutions.Solutions.Count == 0)
        {
            return;
        }

        result.Findings.Add(new AssessmentFinding(
            FindingCategories.Solutions,
            $"{solutions.Solutions.Count} solution(s) and {solutions.Projects.Count} " +
                "project file(s) are stored under this path.",
            "Each solution is a candidate for its own Git repository only to the extent " +
                "that nothing outside its folder is needed to build it.",
            solutions.ProjectsUsingPackagesConfig > 0 ?
                $"{solutions.ProjectsUsingPackagesConfig} project(s) restore packages " +
                    "through packages.config." :
                string.Empty));

        foreach (var reference in solutions.CrossSolutionReferences)
        {
            result.Findings.Add(new AssessmentFinding(
                FindingCategories.Solutions,
                $"{reference.FromProject} references {reference.ToProject}, which is outside " +
                    $"the folder holding {reference.SolutionPath}.",
                "If these solutions are migrated to separate Git repositories, this reference " +
                    "will not resolve."));
        }

        foreach (var shared in solutions.SharedProjects)
        {
            result.Findings.Add(new AssessmentFinding(
                FindingCategories.Solutions,
                $"{shared.ProjectPath} is referenced by projects in {shared.SolutionCount} " +
                    "different solutions.",
                "This code is shared at the source level. It cannot be versioned, built, or " +
                    "shipped independently of the solutions that reference it, and those " +
                    "solutions cannot be migrated to separate Git repositories without " +
                    "breaking these references.",
                string.Join(", ", shared.SolutionPaths)));
        }
    }

    /// <summary>
    /// Where the solution analysis and the build definitions point at the same
    /// folder, both measures of coupling agree, and that is worth saying once
    /// rather than leaving the reader to join two tables.
    /// </summary>
    private void AddCommonCodeFindings(TfvcAssessmentResult result)
    {
        foreach (var folder in result.CommonCodeFolders)
        {
            result.Findings.Add(new AssessmentFinding(
                FindingCategories.SharedCode,
                $"{folder.Path} is referenced like shared code: projects in " +
                    $"{folder.SolutionPaths.Count} solutions reference code under this path, " +
                    $"and {folder.BuildDefinitionNames.Count} build definitions map it into " +
                    "their workspaces.",
                "Every solution and build listed here is coupled to this folder. None of them " +
                    "can be migrated to an independent Git repository while these references " +
                    "exist.",
                "Solutions: " + string.Join(", ", folder.SolutionPaths) +
                    ". Builds: " + string.Join(", ", folder.BuildDefinitionNames) + "."));
        }
    }

    private List<CommonCodeFolder> BuildCommonCodeFolders(TfvcAssessmentResult result)
    {
        var folders = new List<CommonCodeFolder>();

        var multiplyMapped = result.MappedPathUsages
            .Where(x => x.DefinitionCount > 1)
            .ToList();

        foreach (var mapped in multiplyMapped)
        {
            // A shared project sitting under a multiply-mapped folder is the
            // same folder showing up in both signals.
            var solutions = result.Solutions.SharedProjects
                .Where(x => TfvcPath.IsSameOrUnder(x.ProjectPath, mapped.Path) == true)
                .SelectMany(x => x.SolutionPaths)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (solutions.Count == 0)
            {
                continue;
            }

            folders.Add(new CommonCodeFolder
            {
                Path = mapped.Path,
                SolutionPaths = solutions,
                BuildDefinitionNames = mapped.DefinitionNames
            });
        }

        return folders;
    }

    private void AddContentFindings(TfvcAssessmentResult result)
    {
        var content = result.Content;

        if (content.FileCount == 0)
        {
            return;
        }

        result.Findings.Add(new AssessmentFinding(
            FindingCategories.Content,
            $"{content.FileCount} file(s) totalling " +
                $"{TfvcAssessmentReportFormatter.FormatSize(content.TotalSizeBytes)} are stored " +
                $"under {result.ScopePath}.",
            "That figure is the current version of each file. Git carries every version of " +
                "every file in every clone, so the repository that results from a migration " +
                "is larger than this."));

        if (content.FilesOverPushLimit > 0)
        {
            result.Findings.Add(new AssessmentFinding(
                FindingCategories.Content,
                $"{content.FilesOverPushLimit} file(s) are larger than 100 MB.",
                "Files over 100 MB exceed the push limits of most Git hosting services.",
                DescribeLargestFiles(content, TfvcContentScanner.PushLimitSizeBytes)));
        }

        if (content.FilesOverWarningSize > 0)
        {
            result.Findings.Add(new AssessmentFinding(
                FindingCategories.Content,
                $"{content.FilesOverWarningSize} file(s) are larger than 50 MB.",
                "Every clone of the Git repository carries every version of these files.",
                DescribeLargestFiles(content, TfvcContentScanner.WarningSizeBytes)));
        }

        if (content.GeneratedFolders.Count > 0)
        {
            result.Findings.Add(new AssessmentFinding(
                FindingCategories.Content,
                $"{content.GeneratedFolderFileCount} file(s) totalling " +
                    $"{TfvcAssessmentReportFormatter.FormatSize(content.GeneratedFolderSizeBytes)} " +
                    "are stored in folders that normally hold generated output or downloaded " +
                    "dependencies.",
                "These files are carried into the Git repository along with every version of " +
                    "them that was ever checked in.",
                string.Join(", ", content.GeneratedFolders.Select(
                    x => $"{x.Name} ({x.FileCount} file(s))"))));
        }
    }

    private string DescribeLargestFiles(TfvcContentScanResult content, long minimumSizeBytes)
    {
        var matches = content.LargestFiles
            .Where(x => x.SizeBytes >= minimumSizeBytes)
            .Select(x => $"{x.Path} ({TfvcAssessmentReportFormatter.FormatSize(x.SizeBytes)})")
            .ToList();

        return string.Join(", ", matches);
    }

    private void AddBuildDefinitionFindings(TfvcAssessmentResult result)
    {
        if (result.TfvcBuildDefinitions.Count == 0)
        {
            return;
        }

        var detail = result.InactiveBuildDefinitionCount > 0 ?
            $"{result.InactiveBuildDefinitionCount} of them have not completed a run in the " +
                "last 365 days." :
            string.Empty;

        result.Findings.Add(new AssessmentFinding(
            FindingCategories.BuildDefinitions,
            $"{result.TfvcBuildDefinitions.Count} build definition(s) pull source from TFVC.",
            "These builds stop working when TFVC is retired.",
            detail));

        foreach (var definition in result.TfvcBuildDefinitions.Where(x => x.IsComplexMapping == true))
        {
            result.Findings.Add(new AssessmentFinding(
                FindingCategories.BuildDefinitions,
                $"Build definition '{definition.Name}' maps {definition.MappedPaths.Count} " +
                    "separate TFVC paths into its workspace.",
                "A Git-based build pulls from a single repository. This build's source layout " +
                    "cannot be reproduced from a single Git repository.",
                string.Join(", ", definition.MappedPaths)));
        }
    }

    /// <summary>
    /// A folder mapped by several builds and sitting outside the path being
    /// assessed is code that more than one thing depends on.
    /// </summary>
    private void AddSharedFolderFindings(TfvcAssessmentResult result)
    {
        var shared = result.MappedPathUsages
            .Where(x => x.DefinitionCount > 1)
            .Where(x => TfvcPath.IsSameOrUnder(x.Path, result.ScopePath) == false)
            .Where(x => result.CommonCodeFolders.Any(
                folder => TfvcPath.AreEqual(folder.Path, x.Path)) == false)
            .ToList();

        foreach (var usage in shared)
        {
            result.Findings.Add(new AssessmentFinding(
                FindingCategories.SharedFolders,
                $"{usage.Path} is mapped into the workspace of {usage.DefinitionCount} " +
                    "build definitions.",
                "Multiple builds depend on this folder's contents.",
                string.Join(", ", usage.DefinitionNames)));
        }
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
