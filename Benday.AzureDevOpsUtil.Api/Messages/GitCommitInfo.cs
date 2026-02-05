using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class GitCommitInfo
{
    [JsonPropertyName("commitId")]
    public string CommitId { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public GitUserDate Author { get; set; } = new();

    [JsonPropertyName("committer")]
    public GitUserDate Committer { get; set; } = new();

    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("remoteUrl")]
    public string RemoteUrl { get; set; } = string.Empty;
}

public class GitUserDate
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }
}

public class GitCommitListResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("value")]
    public GitCommitInfo[] Value { get; set; } = Array.Empty<GitCommitInfo>();
}
