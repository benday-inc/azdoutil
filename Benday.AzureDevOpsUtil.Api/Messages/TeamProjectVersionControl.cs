using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class TeamProjectVersionControl
{
    [JsonPropertyName("sourceControlType")] 
    public string SourceControlType { get; set; } = string.Empty;

    [JsonPropertyName("gitEnabled")]
    public string GitEnabled { get; set; } = string.Empty;

    [JsonPropertyName("tfvcEnabled")]
    public string TfvcEnabled { get; set; } = string.Empty;
}
