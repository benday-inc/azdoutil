using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages.TaskGroups;

public class TaskGroupStepTaskRef
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("versionSpec")]
    public string VersionSpec { get; set; } = string.Empty;

    [JsonPropertyName("definitionType")]
    public string DefinitionType { get; set; } = string.Empty;
}
