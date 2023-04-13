using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class Field
{
    [JsonPropertyName("defaultValue")]
    public string DefaultValue { get; set; }

    [JsonPropertyName("alwaysRequired")]
    public bool AlwaysRequired { get; set; }
    [JsonPropertyName("referenceName")]
    public string ReferenceName { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("url")]
    public string Url { get; set; }
}
