using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public partial class GetBacklogWorkItemIdsResponse
{
    [JsonPropertyName("workItems")]
    public BacklogWorkItemInfo[] WorkItems { get; set; } = new BacklogWorkItemInfo[0];
}

