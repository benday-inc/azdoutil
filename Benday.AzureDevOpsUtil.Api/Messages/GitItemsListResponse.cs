using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class GitItemsListResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("value")]
    public GitItemInfo[] Value { get; set; } = Array.Empty<GitItemInfo>();
}
