using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages.Releases;


public class Self
{
    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

}



