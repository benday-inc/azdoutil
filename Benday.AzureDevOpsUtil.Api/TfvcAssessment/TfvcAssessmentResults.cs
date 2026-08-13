namespace Benday.AzureDevOpsUtil.Api.TfvcAssessment;

/// <summary>
/// One thing the assessment observed.  A finding is a fact plus what that fact
/// means for a conversion to Git.  There is deliberately no severity, ranking,
/// or recommendation.
/// </summary>
public class AssessmentFinding
{
    public AssessmentFinding()
    {
    }

    public AssessmentFinding(string category, string fact, string consequence, string detail = "")
    {
        Category = category;
        Fact = fact;
        Consequence = consequence;
        Detail = detail;
    }

    public string Category { get; set; } = string.Empty;

    public string Fact { get; set; } = string.Empty;

    public string Consequence { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;
}

public static class FindingCategories
{
    public const string BranchHierarchy = "Branch hierarchy";
    public const string NestedBranches = "Nested branches";
    public const string UnregisteredBranches = "Unregistered branches";
    public const string BranchActivity = "Branch activity";
    public const string BuildDefinitions = "Build definitions";
    public const string SharedFolders = "Folders shared by builds";
}

/// <summary>
/// A build definition that pulls its source from TFVC, and the workspace it
/// builds from.
/// </summary>
public class TfvcBuildDefinitionInfo
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Every mapping line, map and cloak alike, in payload order.</summary>
    public List<TfvcWorkspaceMapping> Mappings { get; set; } = new();

    /// <summary>Distinct server paths brought into the workspace.</summary>
    public List<string> MappedPaths { get; set; } = new();

    /// <summary>Distinct server paths excluded from the workspace.</summary>
    public List<string> CloakedPaths { get; set; } = new();

    public DateTime? LastRunDate { get; set; }

    /// <summary>No completed run within the last year, or no run at all.</summary>
    public bool IsInactive { get; set; }

    /// <summary>
    /// More than one mapped path means the workspace is assembled from several
    /// places, which a single Git repository cannot reproduce.
    /// </summary>
    public bool IsComplexMapping => MappedPaths.Count > 1;
}

/// <summary>
/// A TFVC path and the build definitions that map it into their workspace.
/// </summary>
public class MappedPathUsage
{
    public string Path { get; set; } = string.Empty;

    public List<string> DefinitionNames { get; set; } = new();

    public int DefinitionCount => DefinitionNames.Count;
}

/// <summary>
/// A branch and the branches that were created from it.  This is TFVC branch
/// lineage, which is not the same thing as folder nesting.
/// </summary>
public class TfvcBranchNode
{
    public string Path { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime? CreatedDate { get; set; }

    public List<TfvcBranchNode> Children { get; set; } = new();
}

/// <summary>
/// A branch whose root folder sits inside another branch's root folder.
/// </summary>
public class NestedBranchPair
{
    public string ChildPath { get; set; } = string.Empty;

    public string ParentPath { get; set; } = string.Empty;
}

/// <summary>
/// Sibling folders that look like copies of one another but are not registered
/// as branches.
/// </summary>
public class UnregisteredBranchGroup
{
    public string ParentPath { get; set; } = string.Empty;

    public List<string> FolderPaths { get; set; } = new();
}

public enum BranchActivityClassification
{
    /// <summary>Changesets within the last 90 days.</summary>
    Active,

    /// <summary>No changesets in 90 days, but some within 365 days.</summary>
    Cooling,

    /// <summary>No changesets within 365 days.</summary>
    Dead
}

public class BranchActivity
{
    public string Path { get; set; } = string.Empty;

    public bool IsRegisteredBranch { get; set; }

    public DateTime? LastChangesetDate { get; set; }

    public string LastChangesetAuthor { get; set; } = string.Empty;

    public int ChangesetsLast90Days { get; set; }

    public int ChangesetsLast180Days { get; set; }

    public int ChangesetsLast365Days { get; set; }

    /// <summary>
    /// True when the changeset query hit its cap, which makes the counts a
    /// floor rather than an exact number.
    /// </summary>
    public bool CountsAreCapped { get; set; }

    public BranchActivityClassification Classification { get; set; }
}

public class TfvcAssessmentResult
{
    public string ProjectName { get; set; } = string.Empty;

    public string ScopePath { get; set; } = string.Empty;

    public DateTime GeneratedUtc { get; set; }

    public List<TfvcBranchNode> RegisteredBranchRoots { get; set; } = new();

    public List<string> RegisteredBranchPaths { get; set; } = new();

    public List<NestedBranchPair> NestedBranches { get; set; } = new();

    public List<UnregisteredBranchGroup> UnregisteredBranchGroups { get; set; } = new();

    public List<BranchActivity> BranchActivity { get; set; } = new();

    /// <summary>
    /// Build definitions in the team project that pull source from TFVC.  This
    /// covers the whole project, not just the assessed path.
    /// </summary>
    public List<TfvcBuildDefinitionInfo> TfvcBuildDefinitions { get; set; } = new();

    /// <summary>
    /// Every distinct path mapped by a TFVC-connected build, most-mapped first.
    /// </summary>
    public List<MappedPathUsage> MappedPathUsages { get; set; } = new();

    public List<AssessmentFinding> Findings { get; set; } = new();

    /// <summary>
    /// Anything that limited the scan, such as a depth cap or a changeset count
    /// cap, so the report can say what it did not look at.
    /// </summary>
    public List<string> Notes { get; set; } = new();

    public int ActiveBranchCount =>
        BranchActivity.Count(x => x.Classification == BranchActivityClassification.Active);

    public int CoolingBranchCount =>
        BranchActivity.Count(x => x.Classification == BranchActivityClassification.Cooling);

    public int DeadBranchCount =>
        BranchActivity.Count(x => x.Classification == BranchActivityClassification.Dead);

    public int ComplexBuildDefinitionCount =>
        TfvcBuildDefinitions.Count(x => x.IsComplexMapping == true);

    public int InactiveBuildDefinitionCount =>
        TfvcBuildDefinitions.Count(x => x.IsInactive == true);
}
