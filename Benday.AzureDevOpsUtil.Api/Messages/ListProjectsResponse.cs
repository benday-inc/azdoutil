using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;
public class ListProjectsResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("value")]
    public TeamProjectInfo[] Projects { get; set; } = new TeamProjectInfo[0];
}
