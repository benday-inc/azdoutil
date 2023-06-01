using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class ConnectionDataUserProperties
{
    [JsonPropertyName("Account")]
    public ConnectionDataUserAccount Account { get; set; } = new();
}
