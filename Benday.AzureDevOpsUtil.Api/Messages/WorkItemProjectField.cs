using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class WorkItemProjectField
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("referenceName")]
    public string ReferenceName { get; set; } = string.Empty;
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    [JsonPropertyName("usage")]
    public string Usage { get; set; } = string.Empty;

    [JsonPropertyName("readOnly")]
    public bool ReadOnly { get; set; }
    [JsonPropertyName("canSortBy")]
    public bool CanSortBy { get; set; }
    [JsonPropertyName("isQueryable")]
    public bool IsQueryable { get; set; }

    [JsonPropertyName("supportedOperations")]
    public WorkItemSupportedOperation[] SupportedOperations { get; set; } = new WorkItemSupportedOperation[0];

    [JsonPropertyName("isIdentity")]
    public bool IsIdentity { get; set; }

    [JsonPropertyName("isPicklist")]
    public bool IsPicklist { get; set; }

    [JsonPropertyName("isPicklistSuggested")]
    public bool IsPicklistSuggested { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}