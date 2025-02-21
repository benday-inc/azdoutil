using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using static OfficeOpenXml.ExcelErrorValue;

namespace Benday.AzureDevOpsUtil.Api.Messages.ReleaseDefinitionDetails;
public class GetReleaseDefinitionDetailResponse
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

    [JsonPropertyName("lastRelease")]
    public LastRelease LastRelease { get; set; } = new();

    //[JsonPropertyName("variables")]
    //public Variables Variables { get; set; } = new();

    [JsonPropertyName("environments")]
    public Environment[] Environments { get; set; } = new Environment[0];

    //[JsonPropertyName("artifacts")]
    //public Artifact[] Artifacts { get; set; } = new Artifact[0];

    [JsonPropertyName("triggers")]
    public Trigger[] Triggers { get; set; } = new Trigger[0];

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

    [JsonPropertyName("projectReference")]
    public string ProjectReference { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("_links")]
    public Links Links { get; set; } = new();

    [JsonIgnore]
    public string? RawJson { get; internal set; }
}


public class CreatedBy
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("_links")]
    public Links Links { get; set; } = new();

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("uniqueName")]
    public string UniqueName { get; set; } = string.Empty;

    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("descriptor")]
    public string Descriptor { get; set; } = string.Empty;

}


public class Links
{
    [JsonPropertyName("avatar")]
    public Avatar Avatar { get; set; } = new();

    [JsonPropertyName("self")]
    public Self Self { get; set; } = new();

    [JsonPropertyName("web")]
    public Web Web { get; set; } = new();

}


public class Avatar
{
    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

}


public class ModifiedBy
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("_links")]
    public Links Links { get; set; } = new();

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("uniqueName")]
    public string UniqueName { get; set; } = string.Empty;

    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("descriptor")]
    public string Descriptor { get; set; } = string.Empty;

}


public class LastRelease
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("_links")]
    public Links Links { get; set; } = new();

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("releaseDefinition")]
    public ReleaseDefinition ReleaseDefinition { get; set; } = new();

    [JsonPropertyName("createdOn")]
    public string CreatedOn { get; set; } = string.Empty;

    [JsonPropertyName("createdBy")]
    public CreatedBy CreatedBy { get; set; } = new();

}


public class ReleaseDefinition
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("projectReference")]
    public string ProjectReference { get; set; } = string.Empty;

    [JsonPropertyName("_links")]
    public Links Links { get; set; } = new();

}


public class Variables
{
    [JsonPropertyName("ConnectionString")]
    public ConnectionString ConnectionString { get; set; } = new();

}


public class Environment
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    //[JsonPropertyName("rank")]
    //public int Rank { get; set; }

    //[JsonPropertyName("owner")]
    //public Owner Owner { get; set; } = new();

    //[JsonPropertyName("variables")]
    //public Variables Variables { get; set; } = new();

    //[JsonPropertyName("preDeployApprovals")]
    //public PreDeployApprovals PreDeployApprovals { get; set; } = new();

    //[JsonPropertyName("deployStep")]
    //public DeployStep DeployStep { get; set; } = new();

    //[JsonPropertyName("postDeployApprovals")]
    //public PostDeployApprovals PostDeployApprovals { get; set; } = new();

    [JsonPropertyName("deployPhases")]
    public DeployPhase[] DeployPhases { get; set; } = new DeployPhase[0];

    //[JsonPropertyName("environmentOptions")]
    //public EnvironmentOptions EnvironmentOptions { get; set; } = new();

    //[JsonPropertyName("conditions")]
    //public Condition[] Conditions { get; set; } = new Condition[0];

    //[JsonPropertyName("executionPolicy")]
    //public ExecutionPolicy ExecutionPolicy { get; set; } = new();

    //[JsonPropertyName("currentRelease")]
    //public CurrentRelease CurrentRelease { get; set; } = new();

    //[JsonPropertyName("retentionPolicy")]
    //public RetentionPolicy RetentionPolicy { get; set; } = new();

    //[JsonPropertyName("processParameters")]
    //public ProcessParameters ProcessParameters { get; set; } = new();

    //[JsonPropertyName("properties")]
    //public Properties Properties { get; set; } = new();

    //[JsonPropertyName("preDeploymentGates")]
    //public PreDeploymentGates PreDeploymentGates { get; set; } = new();

    //[JsonPropertyName("postDeploymentGates")]
    //public PostDeploymentGates PostDeploymentGates { get; set; } = new();

    //[JsonPropertyName("badgeUrl")]
    //public string BadgeUrl { get; set; } = string.Empty;

}


public class Owner
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("_links")]
    public Links Links { get; set; } = new();

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("uniqueName")]
    public string UniqueName { get; set; } = string.Empty;

    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("descriptor")]
    public string Descriptor { get; set; } = string.Empty;

}


public class ConnectionString
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

}


public class PreDeployApprovals
{
    [JsonPropertyName("approvals")]
    public Approval[] Approvals { get; set; } = new Approval[0];

    [JsonPropertyName("approvalOptions")]
    public ApprovalOptions ApprovalOptions { get; set; } = new();

}


public class Approval
{
    [JsonPropertyName("rank")]
    public int Rank { get; set; }

    [JsonPropertyName("isAutomated")]
    public bool IsAutomated { get; set; }

    [JsonPropertyName("isNotificationOn")]
    public bool IsNotificationOn { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("approver")]
    public Approver Approver { get; set; } = new();

}


public class ApprovalOptions
{
    [JsonPropertyName("requiredApproverCount")]
    public string RequiredApproverCount { get; set; } = string.Empty;

    [JsonPropertyName("releaseCreatorCanBeApprover")]
    public bool ReleaseCreatorCanBeApprover { get; set; }

    [JsonPropertyName("autoTriggeredAndPreviousEnvironmentApprovedCanBeSkipped")]
    public bool AutoTriggeredAndPreviousEnvironmentApprovedCanBeSkipped { get; set; }

    [JsonPropertyName("enforceIdentityRevalidation")]
    public bool EnforceIdentityRevalidation { get; set; }

    [JsonPropertyName("timeoutInMinutes")]
    public int TimeoutInMinutes { get; set; }

    [JsonPropertyName("executionOrder")]
    public string ExecutionOrder { get; set; } = string.Empty;

}


public class DeployStep
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

}


public class PostDeployApprovals
{
    [JsonPropertyName("approvals")]
    public Approval[] Approvals { get; set; } = new Approval[0];

    [JsonPropertyName("approvalOptions")]
    public ApprovalOptions ApprovalOptions { get; set; } = new();

}


public class Approver
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("_links")]
    public Links Links { get; set; } = new();

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("uniqueName")]
    public string UniqueName { get; set; } = string.Empty;

    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("descriptor")]
    public string Descriptor { get; set; } = string.Empty;

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


public class DeploymentInput
{
    [JsonPropertyName("parallelExecution")]
    public ParallelExecution ParallelExecution { get; set; } = new();

    [JsonPropertyName("agentSpecification")]
    public AgentSpecification AgentSpecification { get; set; } = new();

    [JsonPropertyName("skipArtifactsDownload")]
    public bool SkipArtifactsDownload { get; set; }

 

    [JsonPropertyName("queueId")]
    public int QueueId { get; set; }

    [JsonPropertyName("enableAccessToken")]
    public bool EnableAccessToken { get; set; }

    [JsonPropertyName("timeoutInMinutes")]
    public int TimeoutInMinutes { get; set; }

    [JsonPropertyName("jobCancelTimeoutInMinutes")]
    public int JobCancelTimeoutInMinutes { get; set; }

    [JsonPropertyName("condition")]
    public string Condition { get; set; } = string.Empty;



}


public class ParallelExecution
{
    [JsonPropertyName("parallelExecutionType")]
    public string ParallelExecutionType { get; set; } = string.Empty;

}


public class AgentSpecification
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

}

















public class EnvironmentOptions
{
    [JsonPropertyName("emailNotificationType")]
    public string EmailNotificationType { get; set; } = string.Empty;

    [JsonPropertyName("emailRecipients")]
    public string EmailRecipients { get; set; } = string.Empty;

    [JsonPropertyName("skipArtifactsDownload")]
    public bool SkipArtifactsDownload { get; set; }

    [JsonPropertyName("timeoutInMinutes")]
    public int TimeoutInMinutes { get; set; }

    [JsonPropertyName("enableAccessToken")]
    public bool EnableAccessToken { get; set; }

    [JsonPropertyName("publishDeploymentStatus")]
    public bool PublishDeploymentStatus { get; set; }

    [JsonPropertyName("badgeEnabled")]
    public bool BadgeEnabled { get; set; }

    [JsonPropertyName("autoLinkWorkItems")]
    public bool AutoLinkWorkItems { get; set; }

    [JsonPropertyName("pullRequestDeploymentEnabled")]
    public bool PullRequestDeploymentEnabled { get; set; }

}


public class Condition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("conditionType")]
    public string ConditionType { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

}


public class ExecutionPolicy
{
    [JsonPropertyName("concurrencyCount")]
    public int ConcurrencyCount { get; set; }

    [JsonPropertyName("queueDepthCount")]
    public int QueueDepthCount { get; set; }

}


public class CurrentRelease
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("_links")]
    public Links Links { get; set; } = new();

}


public class RetentionPolicy
{
    [JsonPropertyName("daysToKeep")]
    public int DaysToKeep { get; set; }

    [JsonPropertyName("releasesToKeep")]
    public int ReleasesToKeep { get; set; }

    [JsonPropertyName("retainBuild")]
    public bool RetainBuild { get; set; }

}













public class ArtifactSourceDefinitionUrl
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

}


public class DefaultVersionBranch
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

}


public class DefaultVersionSpecific
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

}


public class DefaultVersionTags
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

}


public class DefaultVersionType
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

}


public class Definition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

}


public class Definitions
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

}


public class IsMultiDefinitionType
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

}


public class Project
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

}


public class Repository
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

}


public class Trigger
{
    [JsonPropertyName("artifactAlias")]
    public string ArtifactAlias { get; set; } = string.Empty;

    [JsonPropertyName("triggerType")]
    public string TriggerType { get; set; } = string.Empty;

}

public class Self
{
    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

}


public class Web
{
    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

}



