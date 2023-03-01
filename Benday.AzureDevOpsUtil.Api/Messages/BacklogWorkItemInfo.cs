using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public partial class BacklogWorkItemInfo
{
    [JsonPropertyName("target")]
    public BacklogWorkItemTargetInfo Target { get; set; } = new();
}

