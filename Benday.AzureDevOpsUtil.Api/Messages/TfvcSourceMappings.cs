using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class TfvcSourceMappings
{
    [JsonPropertyName("mappings")]
    public TfvcSourceMapping[] Mappings { get; set; } = new TfvcSourceMapping[0];

}