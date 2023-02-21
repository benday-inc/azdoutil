using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class WorkItemQueryInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }
    [JsonPropertyName("lastModifiedBy")]
    public LastModifiedBy LastModifiedBy { get; set; }

    [JsonPropertyName("lastModifiedDate")]
    public DateTime LastModifiedDate { get; set; }

    [JsonPropertyName("queryType")]
    public string QueryType { get; set; }

    [JsonPropertyName("columns")]
    public ColumnInfo[] Columns { get; set; }

    [JsonPropertyName("wiql")]
    public string Wiql { get; set; }

    [JsonPropertyName("isPublic")]
    public bool IsPublic { get; set; }

    [JsonPropertyName("lastExecutedBy")]
    public LastExecutedBy LastExecutedBy { get; set; }

    [JsonPropertyName("lastExecutedDate")]
    public DateTime LastExecutedDate { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}
