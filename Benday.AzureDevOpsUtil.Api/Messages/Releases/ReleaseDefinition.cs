using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages.Releases;


public class ReleaseDefinition
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("projectReference")]
    public string ProjectReference { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("_links")]
    public Links Links { get; set; } = new();

}



