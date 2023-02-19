using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class WorkItemQuerySearchResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("value")]
    public WorkItemQueryInfo[] Value { get; set; } = new WorkItemQueryInfo[0];

    [JsonPropertyName("hasMore")]
    public bool HasMore { get; set; }
}