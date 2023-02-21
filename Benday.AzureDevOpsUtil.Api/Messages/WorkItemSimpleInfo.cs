using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public partial class WorkItemSimpleInfo
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("url")]
    public Uri? Url { get; set; }
}
