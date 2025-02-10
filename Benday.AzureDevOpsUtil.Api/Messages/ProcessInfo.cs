using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class ProcessInfo
{
    [JsonPropertyName("type")]
    public int Type { get; set; }
}
