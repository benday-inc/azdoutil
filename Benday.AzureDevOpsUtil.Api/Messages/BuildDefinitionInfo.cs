using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class BuildDefinitionInfo 
{
    [JsonPropertyName("quality")]
    public string Quality { get; set; } = string.Empty;

    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonPropertyName("itemType")]
    public string ItemType { get; set; } = string.Empty;

    [JsonPropertyName("revision")]
    public int Revision { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")] 
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")] 
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("project")]
    public ProjectInfo Project { get; set; } = new();

    [JsonPropertyName("authoredBy")]
    public PersonInfo AuthoredBy { get; set; } = new();
}
