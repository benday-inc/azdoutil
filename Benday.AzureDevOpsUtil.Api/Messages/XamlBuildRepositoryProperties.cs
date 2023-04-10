using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class XamlBuildRepositoryProperties
{
    [JsonPropertyName("tfvcMapping")] 
    public string TfvcMapping { get; set; } = string.Empty;
}
