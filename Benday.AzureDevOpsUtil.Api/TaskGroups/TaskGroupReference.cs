namespace Benday.AzureDevOpsUtil.Api.TaskGroups;

public class TaskGroupReference
{
    public string TaskGroupId { get; set; } = string.Empty;
    public string VersionSpec { get; set; } = string.Empty;
    public int PhaseIndex { get; set; }
    public string PhaseName { get; set; } = string.Empty;
    public int StepIndex { get; set; }
    public string StepDisplayName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}
