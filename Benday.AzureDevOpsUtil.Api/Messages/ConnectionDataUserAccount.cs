using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class ConnectionDataUserAccount
{
    [JsonPropertyName("$type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("$value")]
    public string Value { get; set; } = string.Empty;
}
