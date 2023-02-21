using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public partial class WorkItemQueryExecutionResult
{
    [JsonPropertyName("queryType")]
    public string QueryType { get; set; } = string.Empty;

    [JsonPropertyName("queryResultType")]
    public string QueryResultType { get; set; } = string.Empty;

    [JsonPropertyName("asOf")]
    public DateTimeOffset AsOf { get; set; }

    [JsonPropertyName("columns")]
    public ColumnInfo[] Columns { get; set; } = new ColumnInfo[0];

    [JsonPropertyName("workItems")]
    public WorkItemSimpleInfo[] WorkItems { get; set; } = new WorkItemSimpleInfo[0];
}
