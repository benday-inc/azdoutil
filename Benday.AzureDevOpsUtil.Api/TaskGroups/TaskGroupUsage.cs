namespace Benday.AzureDevOpsUtil.Api.TaskGroups;

public class TaskGroupUsage
{
    public int BuildDefinitionId { get; set; }
    public string BuildDefinitionName { get; set; } = string.Empty;
    public string TaskGroupId { get; set; } = string.Empty;
    public string TaskGroupName { get; set; } = string.Empty;
    public string VersionSpec { get; set; } = string.Empty;
    public int PhaseIndex { get; set; }
    public string PhaseName { get; set; } = string.Empty;
    public int StepIndex { get; set; }
    public string StepDisplayName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}
