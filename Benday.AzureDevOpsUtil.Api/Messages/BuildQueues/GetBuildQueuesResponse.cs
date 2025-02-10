using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Benday.AzureDevOpsUtil.Api.Messages.BuildQueues;
public class GetBuildQueuesResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("value")]
    public BuildQueueInfo[] Value { get; set; } = [];

}


public class BuildQueueInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("projectId")]
    public string ProjectId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("pool")]
    public Pool Pool { get; set; } = new();

    [JsonPropertyName("projectName")]
    public string TeamProjectName { get; set; } = string.Empty;

}


public class Pool
{
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

}


