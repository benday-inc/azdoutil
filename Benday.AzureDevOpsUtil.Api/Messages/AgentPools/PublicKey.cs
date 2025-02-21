using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Benday.AzureDevOpsUtil.Api.Messages.AgentPools;


public class PublicKey
{
    [JsonPropertyName("exponent")]
    public string Exponent { get; set; } = string.Empty;

    [JsonPropertyName("modulus")]
    public string Modulus { get; set; } = string.Empty;

}


