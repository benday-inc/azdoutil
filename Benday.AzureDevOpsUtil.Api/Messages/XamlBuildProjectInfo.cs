using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class XamlBuildProjectInfo
{
    [JsonPropertyName("id")] 
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")] 
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")] 
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("state")] 
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("revision")] 
    public int Revision { get; set; }
}
