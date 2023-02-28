using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class TeamProjectVersionControlForCreate
{
    [JsonPropertyName("sourceControlType")]
    public string SourceControlType { get; set; } = string.Empty;
}
