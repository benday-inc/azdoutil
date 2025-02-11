using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages.Releases;


public class ReleaseInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("createdOn")]
    public string CreatedOn { get; set; } = string.Empty;

    [JsonPropertyName("modifiedOn")]
    public string ModifiedOn { get; set; } = string.Empty;

    [JsonPropertyName("modifiedBy")]
    public ModifiedBy ModifiedBy { get; set; } = new();

    [JsonPropertyName("createdBy")]
    public CreatedBy CreatedBy { get; set; } = new();

    [JsonPropertyName("createdFor")]
    public CreatedFor CreatedFor { get; set; } = new();

    [JsonPropertyName("variables")]
    public ReleaseVariables Variables { get; set; } = new();

    [JsonPropertyName("releaseDefinition")]
    public ReleaseDefinition ReleaseDefinition { get; set; } = new();

    [JsonPropertyName("releaseDefinitionRevision")]
    public int ReleaseDefinitionRevision { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("releaseNameFormat")]
    public string ReleaseNameFormat { get; set; } = string.Empty;

    [JsonPropertyName("keepForever")]
    public bool KeepForever { get; set; }

    [JsonPropertyName("definitionSnapshotRevision")]
    public int DefinitionSnapshotRevision { get; set; }

    [JsonPropertyName("logsContainerUrl")]
    public string LogsContainerUrl { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("_links")]
    public Links Links { get; set; } = new();

    [JsonPropertyName("triggeringArtifactAlias")]
    public string TriggeringArtifactAlias { get; set; } = string.Empty;

    [JsonPropertyName("projectReference")]
    public ProjectReference ProjectReference { get; set; } = new();

    [JsonPropertyName("properties")]
    public ReleaseProperties Properties { get; set; } = new();
    public GetReleaseDetailResponse? Details { get; set; }

}



