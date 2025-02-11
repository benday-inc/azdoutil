using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages.Releases;



public class GetReleaseDetailResponse
{
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("revision")]
    public int Revision { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("createdBy")]
    public CreatedBy CreatedBy { get; set; } = new();

    [JsonPropertyName("createdOn")]
    public string CreatedOn { get; set; } = string.Empty;

    [JsonPropertyName("modifiedBy")]
    public ModifiedBy ModifiedBy { get; set; } = new();

    [JsonPropertyName("modifiedOn")]
    public string ModifiedOn { get; set; } = string.Empty;

    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; set; }

    [JsonPropertyName("isDisabled")]
    public bool IsDisabled { get; set; }

    //[JsonPropertyName("lastRelease")]
    //public LastRelease LastRelease { get; set; } = new();

    //[JsonPropertyName("variables")]
    //public Variables Variables { get; set; } = new();

    [JsonPropertyName("environments")]
    public Environment[] Environments { get; set; } = [];

    //[JsonPropertyName("artifacts")]
    //public Artifact[] Artifacts { get; set; } = new Artifact[0];

    //[JsonPropertyName("triggers")]
    //public Trigger[] Triggers { get; set; } = new Trigger[0];

    [JsonPropertyName("releaseNameFormat")]
    public string ReleaseNameFormat { get; set; } = string.Empty;

    //[JsonPropertyName("properties")]
    //public Properties Properties { get; set; } = new();

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    //[JsonPropertyName("projectReference")]
    //public string ProjectReference { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("_links")]
    public Links Links { get; set; } = new();

    /// <summary>
    /// Used to store the raw JSON from the API call, if needed.
    /// </summary>
    [JsonIgnore]
    public string? RawJson { get; set; }

}

public class Environment
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("rank")]
    public int Rank { get; set; }


    [JsonPropertyName("deployPhases")]
    public DeployPhase[] DeployPhases { get; set; } = new DeployPhase[0];


}


public class DeployPhase
{
    [JsonPropertyName("deploymentInput")]
    public DeploymentInput DeploymentInput { get; set; } = new();

    [JsonPropertyName("rank")]
    public int Rank { get; set; }

    [JsonPropertyName("phaseType")]
    public string PhaseType { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("refName")]
    public string RefName { get; set; } = string.Empty;


}

public class AgentSpecification
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

}


public class DeploymentInput
{
    [JsonPropertyName("agentSpecification")]
    public AgentSpecification AgentSpecification { get; set; } = new();

    [JsonPropertyName("queueId")]
    public int QueueId { get; set; }

    
}