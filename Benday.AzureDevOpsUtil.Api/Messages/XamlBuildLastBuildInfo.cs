using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class XamlBuildLastBuildInfo
{
    [JsonPropertyName("id")] 
    public int Id { get; set; }

    [JsonPropertyName("url")] 
    public string Url { get; set; } = string.Empty;
}
