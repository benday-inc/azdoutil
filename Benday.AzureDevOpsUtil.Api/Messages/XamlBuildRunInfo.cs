using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class XamlBuildRunInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("buildNumber")]
    public string BuildNumber { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    [JsonPropertyName("queueTime")]
    public DateTime QueueTime { get; set; }

    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; }

    [JsonPropertyName("finishTime")]
    public DateTime FinishTime { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("project")]
    public XamlBuildProjectInfo Project { get; set; } = new();

    [JsonPropertyName("sourceVersion")]
    public string SourceVersion { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string BuildReason { get; set; } = string.Empty;

    [JsonPropertyName("lastChangedDate")]
    public DateTime LastChangedDate { get; set; }
}
