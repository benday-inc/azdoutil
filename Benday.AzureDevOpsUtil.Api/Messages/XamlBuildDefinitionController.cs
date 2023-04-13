using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class XamlBuildDefinitionController
{
    [JsonPropertyName("uri")] 
    public string Uri { get; set; } = string.Empty;

    [JsonPropertyName("status")] 
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("enabled")] 
    public bool Enabled { get; set; }

    [JsonPropertyName("createdDate")] 
    public DateTime CreatedDate { get; set; }

    [JsonPropertyName("updatedDate")] 
    public DateTime UpdatedDate { get; set; }

    [JsonPropertyName("id")] 
    public int Id { get; set; }

    [JsonPropertyName("name")] 
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")] 
    public string Url { get; set; } = string.Empty;
}
