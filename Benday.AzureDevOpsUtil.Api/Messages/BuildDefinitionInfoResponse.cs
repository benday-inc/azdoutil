using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class BuildDefinitionInfoResponse
{
    public BuildDefinitionInfoResponse()
    {
        Values = new List<BuildDefinitionInfo>();
    }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("value")]
    public List<BuildDefinitionInfo> Values { get; set; }
}