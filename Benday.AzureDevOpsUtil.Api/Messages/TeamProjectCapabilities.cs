using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class TeamProjectCapabilities
{
    [JsonPropertyName("versioncontrol")]
    public TeamProjectVersionControl VersionControl { get; set; } = new();

    [JsonPropertyName("processTemplate")]
    public TeamProjectProcessTemplate ProcessTemplate { get; set; } = new();
}
