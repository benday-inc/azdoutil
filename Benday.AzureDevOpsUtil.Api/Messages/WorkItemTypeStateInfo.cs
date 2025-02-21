using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class WorkItemTypeStateInfo
{
    [JsonPropertyName("name")] 
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    [JsonPropertyName("category")] 
    public string Category { get; set; } = string.Empty;
}

