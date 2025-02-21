using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages.AgentPools;

public class Authorization
{
    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("publicKey")]
    public PublicKey PublicKey { get; set; } = new();

}


