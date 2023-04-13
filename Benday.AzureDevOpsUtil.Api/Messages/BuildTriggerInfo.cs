using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class BuildTriggerInfo
{
    [JsonPropertyName("ci.sourceBranch")]
    public string SourceBranch { get; set; } = string.Empty;


    [JsonPropertyName("ci.sourceSha")]
    public string SourceSha{ get; set; } = string.Empty;


    [JsonPropertyName("ci.message")]
    public string Message { get; set; } = string.Empty;


    [JsonPropertyName("ci.triggerRepository")]
    public string TriggerRepository { get; set; } = string.Empty;

}




