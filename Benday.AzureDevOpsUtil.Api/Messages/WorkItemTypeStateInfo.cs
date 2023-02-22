using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class WorkItemTypeStateInfo
{
    [JsonPropertyName("name")] 
    public string Name { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; }

    [JsonPropertyName("category")] 
    public string Category { get; set; }
}

