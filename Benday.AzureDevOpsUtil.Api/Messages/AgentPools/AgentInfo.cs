using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages.AgentPools;


public class AgentInfo
{

    [JsonPropertyName("maxParallelism")]
    public int MaxParallelism { get; set; }

    [JsonPropertyName("createdOn")]
    public string CreatedOn { get; set; } = string.Empty;

    [JsonPropertyName("statusChangedOn")]
    public string StatusChangedOn { get; set; } = string.Empty;

    [JsonPropertyName("authorization")]
    public Authorization Authorization { get; set; } = new();

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("osDescription")]
    public string OperatingSystem { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("provisioningState")]
    public string ProvisioningState { get; set; } = string.Empty;

    [JsonPropertyName("accessPoint")]
    public string AccessPoint { get; set; } = string.Empty;

}


