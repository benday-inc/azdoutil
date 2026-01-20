using System.Text.Json.Serialization;

using Benday.AzureDevOpsUtil.Api.Commands.WorkItems;
using Benday.AzureDevOpsUtil.Api.WorkItems;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class WorkItemTypeDefinitionResponse
{

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("referenceName")]
    public string ReferenceName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public WorkItemIcon Icon { get; set; } = new();

    [JsonPropertyName("isDisabled")]
    public bool IsDisabled { get; set; }

    [JsonPropertyName("xmlForm")]
    public string XmlForm { get; set; } = string.Empty;

    [JsonPropertyName("fields")]
    public Field[] Fields { get; set; } = [];

    [JsonPropertyName("fieldInstances")]
    public Field[] FieldInstances { get; set; } = [];

    [JsonPropertyName("states")]
    public State[] States { get; set; } = [];

    [JsonPropertyName("transitions")]
    public Dictionary<string, WorkItemStateTransition[]> Transitions { get; set; } = new();

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// This is used when getting work item type def 
    /// purely for convience
    /// </summary>
    [JsonIgnore]
    public GetWorkItemFieldsCommand? FieldDetails { get; set; }

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
