using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class TeamProjectVersionControl
{
    [JsonPropertyName("sourceControlType")] 
    public string SourceControlType { get; set; } = string.Empty;
}
