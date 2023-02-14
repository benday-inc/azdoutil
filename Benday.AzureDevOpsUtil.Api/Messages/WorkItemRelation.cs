using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;
public class WorkItemRelation : IWorkItemRelation
{
    private WorkItemRelationAttributes _attributes;

    [JsonPropertyName("rel")]
    public string RelationType { get; set; }

    [JsonPropertyName("url")]
    public string RelationUrl { get; set; }

    [JsonPropertyName("attributes")]
    public WorkItemRelationAttributes Attributes
    {
        get
        {
            if (_attributes == null) _attributes = new WorkItemRelationAttributes();
            return _attributes;
        }
        set => _attributes = value;
    }
}
