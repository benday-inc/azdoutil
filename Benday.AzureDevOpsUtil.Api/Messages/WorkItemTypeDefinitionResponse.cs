using System.Text.Json.Serialization;

using Benday.AzureDevOpsUtil.Api.WorkItems;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class WorkItemTypeDefinitionResponse
{

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("referenceName")]
    public string ReferenceName { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; }

    [JsonPropertyName("icon")]
    public WorkItemIcon Icon { get; set; }

    [JsonPropertyName("isDisabled")]
    public bool IsDisabled { get; set; }

    [JsonPropertyName("xmlForm")]
    public string XmlForm { get; set; }

    [JsonPropertyName("fields")]
    public Field[] Fields { get; set; }

    [JsonPropertyName("fieldInstances")]
    public Field[] FieldInstances { get; set; }

    [JsonPropertyName("states")]
    public State[] States { get; set; }

    [JsonPropertyName("transitions")]
    public Dictionary<string, WorkItemStateTransition[]> Transitions { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    /// <summary>
    /// This is used when getting work item type def 
    /// purely for convience
    /// </summary>
    [JsonIgnore]
    public GetWorkItemFieldsCommand FieldDetails { get; set; }

    internal bool HasState(string state)
    {
        var match = States.Where(x => x.Name == state).Any();

        return match;
    }

    internal bool HasField(string refname)
    {
        var match = Fields.Where(x => x.Name == refname).Any();

        return match;
    }
}
