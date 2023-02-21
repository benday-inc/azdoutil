using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public partial class WorkItemDetailResponse
{
    [JsonPropertyName("count")]
    public long Count { get; set; }

    [JsonPropertyName("value")]
    public GetWorkItemByIdResponse[] Value { get; set; }
}