using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class Field
{
    [JsonPropertyName("defaultValue")]
    public string DefaultValue { get; set; } = string.Empty;

    [JsonPropertyName("alwaysRequired")]
    public bool AlwaysRequired { get; set; }
    [JsonPropertyName("referenceName")]
    public string ReferenceName { get; set; } = string.Empty;
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("url")]
    public string Url { get; set; }= string.Empty;
}
