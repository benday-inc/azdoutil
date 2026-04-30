using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages.TaskGroups;

public class TaskGroupListResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("value")]
    public List<TaskGroupInfo> Values { get; set; } = new();
}
