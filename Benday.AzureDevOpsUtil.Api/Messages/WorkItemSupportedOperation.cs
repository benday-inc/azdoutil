using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class WorkItemSupportedOperation
{
    [JsonPropertyName("referenceName")]
    public string ReferenceName { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}
