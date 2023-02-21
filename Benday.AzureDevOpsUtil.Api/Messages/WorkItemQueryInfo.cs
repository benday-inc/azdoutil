using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class WorkItemQueryInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }
    [JsonPropertyName("lastModifiedBy")]
    public LastModifiedBy LastModifiedBy { get; set; } = new();

    [JsonPropertyName("lastModifiedDate")]
    public DateTime LastModifiedDate { get; set; }

    [JsonPropertyName("queryType")]
    public string QueryType { get; set; } = string.Empty;

    [JsonPropertyName("columns")]
    public ColumnInfo[] Columns { get; set; } = new ColumnInfo[0];

    [JsonPropertyName("wiql")]
    public string Wiql { get; set; } = string.Empty;

    [JsonPropertyName("isPublic")]
    public bool IsPublic { get; set; }

    [JsonPropertyName("lastExecutedBy")]
    public LastExecutedBy LastExecutedBy { get; set; } = new();

    [JsonPropertyName("lastExecutedDate")]
    public DateTime LastExecutedDate { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
