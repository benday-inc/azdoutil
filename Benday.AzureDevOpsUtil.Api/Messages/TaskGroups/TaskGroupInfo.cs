using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages.TaskGroups;

public class TaskGroupInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("friendlyName")]
    public string FriendlyName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("definitionType")]
    public string DefinitionType { get; set; } = string.Empty;

    [JsonPropertyName("revision")]
    public int Revision { get; set; }

    [JsonPropertyName("version")]
    public TaskGroupVersion Version { get; set; } = new();

    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;

    [JsonPropertyName("createdOn")]
    public DateTimeOffset? CreatedOn { get; set; }

    [JsonPropertyName("modifiedOn")]
    public DateTimeOffset? ModifiedOn { get; set; }

    [JsonPropertyName("createdBy")]
    public PersonInfo CreatedBy { get; set; } = new();

    [JsonPropertyName("modifiedBy")]
    public PersonInfo ModifiedBy { get; set; } = new();

    [JsonPropertyName("tasks")]
    public List<TaskGroupStep> Tasks { get; set; } = new();

    [JsonPropertyName("inputs")]
    public List<TaskGroupParameter> Inputs { get; set; } = new();

    [JsonPropertyName("instanceNameFormat")]
    public string InstanceNameFormat { get; set; } = string.Empty;
}
