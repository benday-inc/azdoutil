using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages.AgentPools;


public class AgentPool
{
    [JsonPropertyName("createdOn")]
    public string CreatedOn { get; set; } = string.Empty;

    [JsonPropertyName("autoProvision")]
    public bool AutoProvision { get; set; }

    [JsonPropertyName("autoUpdate")]
    public bool AutoUpdate { get; set; }

    [JsonPropertyName("autoSize")]
    public bool AutoSize { get; set; }

    [JsonPropertyName("targetSize")]
    public int? TargetSize { get; set; }

    [JsonPropertyName("agentCloudId")]
    public int? AgentCloudId { get; set; }

    [JsonPropertyName("createdBy")]
    public Owner? CreatedBy { get; set; } = new();

    [JsonPropertyName("owner")]
    public Owner? Owner { get; set; } = new();

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("isHosted")]
    public bool IsHosted { get; set; }

    [JsonPropertyName("poolType")]
    public string PoolType { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("isLegacy")]
    public bool IsLegacy { get; set; }

    [JsonPropertyName("options")]
    public string Options { get; set; } = string.Empty;

    [JsonPropertyName("agents")]
    public GetAgentsByPoolIdResponse? Agents { get; set; }

}

