namespace Benday.AzureDevOpsUtil.Api.BranchHealth;

public class BranchInfo
{
    public string Name { get; set; } = string.Empty;

    public bool IsDefaultBranch { get; set; }

    public int AheadCount { get; set; }

    public int BehindCount { get; set; }

    /// <summary>
    /// When the branch last received a commit.  This is the committer date, not
    /// the author date: an author date survives a rebase and can be set to
    /// anything, so it says nothing about when work landed here.
    /// </summary>
    public DateTime? LastCommitDate { get; set; }

    public string LastCommitBy { get; set; } = string.Empty;

    /// <summary>Days between the last commit and the time of the report.</summary>
    public double? AgeInDays { get; set; }

    /// <summary>Carries commits the default branch does not have.</summary>
    public bool IsUnmerged => AheadCount > 0;
}

public class CommitterActivity
{
    public string Name { get; set; } = string.Empty;

    public List<string> BranchNames { get; set; } = new();

    public int BranchCount => BranchNames.Count;
}

public class BranchHealthResult
{
    public string ProjectName { get; set; } = string.Empty;

    public string RepositoryName { get; set; } = string.Empty;

    public DateTime GeneratedUtc { get; set; }

    /// <summary>The window the report treats as "active", in days.</summary>
    public int ActivityWindowDays { get; set; }

    public List<BranchInfo> Branches { get; set; } = new();

    /// <summary>
    /// People with a branch that received a commit in the last 14 days, and the
    /// branches they touched.
    /// </summary>
    public List<CommitterActivity> Committers { get; set; } = new();

    public List<string> Notes { get; set; } = new();

    public int BranchCount => Branches.Count;

    public int ActiveBranchCount { get; set; }

    public int ActiveBranchCountLast30Days { get; set; }

    public int UnmergedBranchCount =>
        Branches.Count(x => x.IsDefaultBranch == false && x.IsUnmerged == true);

    public int DeadBranchCount { get; set; }

    /// <summary>
    /// Median age of the unmerged branches, excluding the default branch.  Null
    /// when there are none.
    /// </summary>
    public double? MedianUnmergedBranchAgeInDays { get; set; }

    public BranchInfo? OldestUnmergedBranch { get; set; }
}
