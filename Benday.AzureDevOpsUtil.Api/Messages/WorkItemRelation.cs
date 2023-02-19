using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;
public class WorkItemRelation : IWorkItemRelation
{

    [JsonPropertyName("rel")]
    public string RelationType { get; set; } = string.Empty;

    [JsonPropertyName("url")] public string RelationUrl { get; set; } = string.Empty;

    [JsonPropertyName("attributes")]
    public WorkItemRelationAttributes Attributes { get; set; } = new();
}
