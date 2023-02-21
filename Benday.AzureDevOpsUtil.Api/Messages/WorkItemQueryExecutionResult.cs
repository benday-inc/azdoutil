using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public partial class WorkItemQueryExecutionResult
{
    [JsonPropertyName("queryType")]
    public string QueryType { get; set; }

    [JsonPropertyName("queryResultType")]
    public string QueryResultType { get; set; }

    [JsonPropertyName("asOf")]
    public DateTimeOffset AsOf { get; set; }

    [JsonPropertyName("columns")]
    public ColumnInfo[] Columns { get; set; }

    [JsonPropertyName("workItems")]
    public WorkItemSimpleInfo[] WorkItems { get; set; }
}
