using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class CreateInheritedWorkItemTypeResponse
{
    [JsonPropertyName("referenceName")]
    public string RefName { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("inherits")]
    public string InheritsFromWorkItemRefName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string Color { get; set; } = "009CCC";

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "icon_list";

    [JsonPropertyName("isDisabled")]
    public bool IsDisabled { get; set; }
}
