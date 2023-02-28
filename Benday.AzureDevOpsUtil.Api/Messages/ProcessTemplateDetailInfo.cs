using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class ProcessTemplateDetailInfo
{
    [JsonPropertyName("typeId")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; } = false;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("parentProcessTypeId")]
    public string ParentProcessTypeId { get; set; } = string.Empty;

    [JsonIgnore]
    public string ParentProcessName { get; set; } = string.Empty;

    [JsonIgnore]
    public ProcessTemplateDetailInfo? Parent { get; set; }

    [JsonPropertyName("referenceName")]
    public string ReferenceName { get; set; } = string.Empty;

    [JsonPropertyName("customizationType")]
    public string CustomizationType { get; set; } = string.Empty;
}