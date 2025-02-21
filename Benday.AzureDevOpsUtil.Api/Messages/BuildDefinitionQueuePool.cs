using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;


public class BuildDefinitionQueuePool
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("isHosted")]
    public bool IsHosted { get; set; }

}