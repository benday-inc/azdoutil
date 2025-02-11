using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages.Releases;


public class Links
{
    [JsonPropertyName("avatar")]
    public Avatar Avatar { get; set; } = new();

    [JsonPropertyName("self")]
    public Self Self { get; set; } = new();

    [JsonPropertyName("web")]
    public Web Web { get; set; } = new();

}



