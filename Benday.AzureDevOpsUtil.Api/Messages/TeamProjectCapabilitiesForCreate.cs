using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class TeamProjectCapabilitiesForCreate
{
    [JsonPropertyName("versioncontrol")]
    public TeamProjectVersionControlForCreate VersionControl { get; set; } = new();

    [JsonPropertyName("processTemplate")]
    public TeamProjectProcessTemplateForCreate ProcessTemplate { get; set; } = new();
}