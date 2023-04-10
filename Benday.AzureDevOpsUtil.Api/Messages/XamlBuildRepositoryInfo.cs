using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class XamlBuildRepositoryInfo
{

    [JsonPropertyName("properties")]
    public XamlBuildRepositoryProperties Properties { get; set; } = new();

    [JsonPropertyName("type")] 
    public string RepositoryType { get; set; } = string.Empty;

    [JsonPropertyName("clean")] 
    public object Clean { get; set; } = string.Empty;

    [JsonPropertyName("checkoutSubmodules")] 
    public bool CheckoutSubmodules { get; set; }
}
