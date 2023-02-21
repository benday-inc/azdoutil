using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public partial class WorkItemQueryRequestBody
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;
}