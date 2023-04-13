using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class BuildRunInfo
{
    [JsonPropertyName("triggerInfo")]
    public BuildTriggerInfo TriggerInfo { get; set; } = new();
    [JsonPropertyName("id")] 
    public int Id { get; set; }
    [JsonPropertyName("buildNumber")] 
    public string BuildNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public DateTime QueueTime { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime FinishTime { get; set; }
    public string Url { get; set; } = string.Empty;
}




