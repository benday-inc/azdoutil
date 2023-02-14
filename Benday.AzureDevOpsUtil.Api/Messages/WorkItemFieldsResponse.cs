using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class WorkItemFieldsResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }


    [JsonPropertyName("value")]
    public WorkItemFieldInfo[] Fields { get; set; } = new WorkItemFieldInfo[0];
}
