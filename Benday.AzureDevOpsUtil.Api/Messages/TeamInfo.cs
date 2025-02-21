using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class TeamInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("identityUrl")]
    public string IdentityUrl { get; set; } = string.Empty;
}

