using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages.AgentPools;
public class GetAgentsByPoolIdResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("value")]
    public AgentInfo[] Value { get; set; } = new AgentInfo[0];

}


