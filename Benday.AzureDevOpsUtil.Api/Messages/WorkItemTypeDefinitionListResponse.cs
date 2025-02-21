using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class WorkItemTypeDefinitionListResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }


    [JsonPropertyName("value")]
    public WorkItemTypeDefinitionResponse[] Types { get; set; } = [];
}

