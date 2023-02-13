using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;


public class WorkItemFieldInfo
{
    [JsonPropertyName("defaultValue")]
    public string DefaultValue { get; set; }=String.Empty;

    [JsonPropertyName("allowedValues")]
    public string[] AllowedValues { get; set; }= new string[0];

    [JsonPropertyName("alwaysRequired")]
    public bool AlwaysRequired { get; set; }

    [JsonPropertyName("dependentFields")]
    public WorkItemDependentFieldInfo[] DependentFields { get; set; } 

    [JsonPropertyName("referenceName")]
    public string ReferenceName { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("url")]
    public string Url { get; set; }

    public string DataType { get; set; }
}
