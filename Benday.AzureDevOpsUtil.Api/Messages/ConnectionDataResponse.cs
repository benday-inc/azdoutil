using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;


public class ConnectionDataResponse
{
    [JsonPropertyName("authenticatedUser")]
    public ConnectionDataUserInfo AuthenticatedUser { get; set; } = new();

    [JsonPropertyName("authorizedUser")]
    public ConnectionDataUserInfo AuthorizedUser { get; set; } = new();

    [JsonPropertyName("instanceId")]
    public string InstanceId { get; set; } = string.Empty;

    [JsonPropertyName("deploymentId")]
    public string DeploymentId { get; set; } = string.Empty;

    [JsonPropertyName("deploymentType")]
    public string DeploymentType { get; set; } = string.Empty;

    [JsonPropertyName("locationServiceData")]
    public ConnectionDataLocationServiceInfo LocationServiceData { get; set; } = new();

    [JsonPropertyName("webApplicationRelativeDirectory")]
    public string WebApplicationRelativeDirectory { get; set; } = string.Empty;
}
