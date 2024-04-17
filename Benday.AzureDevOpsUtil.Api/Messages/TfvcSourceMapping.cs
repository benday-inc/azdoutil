using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class TfvcSourceMapping
{
    [JsonPropertyName("serverPath")]
    public string ServerPath { get; set; } = string.Empty;

    [JsonPropertyName("mappingType")]
    public string MappingType { get; set; } = string.Empty;

    [JsonPropertyName("localPath")]
    public string LocalPath { get; set; } = string.Empty;

}
