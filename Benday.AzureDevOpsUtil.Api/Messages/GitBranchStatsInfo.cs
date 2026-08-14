using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

/// <summary>
/// Ahead and behind counts for one branch, as returned by
/// GET .../git/repositories/{repo}/stats/branches.  One call covers every
/// branch in the repository.
/// </summary>
public class GitBranchStatsInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Commits on this branch that are not on the default branch.</summary>
    [JsonPropertyName("aheadCount")]
    public int AheadCount { get; set; }

    [JsonPropertyName("behindCount")]
    public int BehindCount { get; set; }

    /// <summary>True for the branch everything else is compared against.</summary>
    [JsonPropertyName("isBaseVersion")]
    public bool IsBaseVersion { get; set; }

    [JsonPropertyName("commit")]
    public GitCommitStatsInfo? Commit { get; set; }
}

public class GitCommitStatsInfo
{
    [JsonPropertyName("commitId")]
    public string CommitId { get; set; } = string.Empty;

    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public GitUserDateInfo? Author { get; set; }

    [JsonPropertyName("committer")]
    public GitUserDateInfo? Committer { get; set; }
}

public class GitUserDateInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public DateTime? Date { get; set; }
}

public class GitBranchStatsListResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("value")]
    public List<GitBranchStatsInfo> Value { get; set; } = new();
}
