using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class GitRepositoryList
{
    [JsonPropertyName("value")]
    public GitRepositoryInfo[] Value { get; set; } = new GitRepositoryInfo[0];
    
    [JsonPropertyName("count")]
    public int Count { get; set; }

}