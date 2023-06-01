using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Benday.AzureDevOpsUtil.Api.Messages;
public class GetGroupsResponse
{
    [JsonPropertyName("identities")]
    public IdentityInfo[] Identities { get; set; } = new IdentityInfo[] { };

    [JsonPropertyName("hasMore")] 
    public bool hasMore { get; set; }
    
    [JsonPropertyName("totalIdentityCount")] 
    public int totalIdentityCount { get; set; }
}
