using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.DeploymentGroups;

// -- API response shapes (distributedtask/deploymentgroups and .../targets) --

public class DeploymentGroupListResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("value")]
    public List<DeploymentGroupInfo> Value { get; set; } = new();
}

public class DeploymentGroupInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("machineCount")]
    public int MachineCount { get; set; }
}

public class DeploymentTargetListResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("value")]
    public List<DeploymentTargetInfo> Value { get; set; } = new();
}

public class DeploymentTargetInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("agent")]
    public DeploymentTargetAgentInfo? Agent { get; set; }
}

public class DeploymentTargetAgentInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
}

// -- Report shapes produced by the analyzer --

public class DeploymentGroupUsageReport
{
    public List<DeploymentGroupUsageProject> Projects { get; set; } = new();
}

public class DeploymentGroupUsageProject
{
    public string ProjectName { get; set; } = string.Empty;

    public List<DeploymentGroupUsage> Groups { get; set; } = new();

    /// <summary>
    /// Deployment group phases whose queueId does not match any deployment
    /// group in the project -- usually a group that was deleted after the
    /// release definition was written.
    /// </summary>
    public List<DeploymentGroupPhaseReference> PhasesWithUnknownGroup { get; set; } = new();
}

public class DeploymentGroupUsage
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<DeploymentTargetInfo> Targets { get; set; } = new();
    public List<DeploymentGroupPhaseUsage> Consumers { get; set; } = new();
}

/// <summary>A deployment group phase found in a release definition.</summary>
public class DeploymentGroupPhaseReference
{
    public int ReleaseDefinitionId { get; set; }
    public string ReleaseDefinitionName { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = string.Empty;
    public string PhaseName { get; set; } = string.Empty;

    /// <summary>
    /// For a machineGroupBasedDeployment phase, deploymentInput.queueId is the
    /// deployment group id.
    /// </summary>
    public int DeploymentGroupId { get; set; }

    /// <summary>Tag filter on the phase. Empty means every target in the group.</summary>
    public List<string> Tags { get; set; } = new();
}

public class DeploymentGroupPhaseUsage : DeploymentGroupPhaseReference
{
    /// <summary>
    /// The targets in the deployment group that satisfy the phase's tag filter
    /// (a target must carry every tag the phase names).
    /// </summary>
    public List<string> MatchingTargetNames { get; set; } = new();
}
