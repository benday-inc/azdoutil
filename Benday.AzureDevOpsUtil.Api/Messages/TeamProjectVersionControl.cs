using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class TeamProjectVersionControl
{
    [JsonPropertyName("sourceControlType")] public string SourceControlType { get; set; } = string.Empty;
}
public class ClassificationNode
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("identifier")]
    public string Identifier { get; set; }= string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; }=string.Empty;
    [JsonPropertyName("structureType")]
    public string StructureType { get; set; } = string.Empty;

    [JsonPropertyName("hasChildren")]
    public bool HasChildren { get; set; }
    [JsonPropertyName("path")]
    public string Path { get; set; } =string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("children")]
    public ClassificationNodeChild[] Children { get; set; } = new ClassificationNodeChild[0];
}
