using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class CreateInheritedWorkItemTypeRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("inheritsFrom")]
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

public class CreateWorkItemStateRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string Color { get; set; } = "b2b2b2";

    [JsonPropertyName("stateCategory")]
    public string StateCategory { get; set; } = string.Empty;
}

public class CreateWorkItemStateResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string Color { get; set; } = "b2b2b2";

    [JsonPropertyName("stateCategory")]
    public string StateCategory { get; set; } = string.Empty;

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("customizationType")]
    public string CustomizationType { get; set; } = string.Empty;

}
