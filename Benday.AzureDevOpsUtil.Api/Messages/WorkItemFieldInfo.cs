using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;


public class WorkItemFieldInfo
{
    [JsonPropertyName("defaultValue")]
    public string DefaultValue { get; set; } = String.Empty;

    [JsonPropertyName("allowedValues")]
    public string[] AllowedValues { get; set; } = new string[0];

    [JsonPropertyName("alwaysRequired")]
    public bool AlwaysRequired { get; set; }

    [JsonPropertyName("dependentFields")]
    public WorkItemDependentFieldInfo[] DependentFields { get; set; } = new WorkItemDependentFieldInfo[0];

    [JsonPropertyName("referenceName")]
    public string ReferenceName { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    public string DataType { get; set; } = string.Empty;
}
