using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class GitItemInfo
{
    [JsonPropertyName("objectId")]
    public string ObjectId { get; set; } = string.Empty;

    [JsonPropertyName("gitObjectType")]
    public string GitObjectType { get; set; } = string.Empty;

    [JsonPropertyName("commitId")]
    public string CommitId { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("isFolder")]
    public bool IsFolder { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
