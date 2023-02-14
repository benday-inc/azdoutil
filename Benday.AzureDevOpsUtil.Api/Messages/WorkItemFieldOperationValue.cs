using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class WorkItemFieldOperationValue
{
    [JsonPropertyName("op")]
    public string Operation { get; set; } = "add";

    [JsonPropertyName("path")] public string Path { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public object Value { get; set; } = string.Empty;

    [JsonIgnore]
    public string Refname { get; set; }= string.Empty;
}