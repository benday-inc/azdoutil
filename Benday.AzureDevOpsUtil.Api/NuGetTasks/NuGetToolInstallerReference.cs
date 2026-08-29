namespace Benday.AzureDevOpsUtil.Api.NuGetTasks;

public class NuGetToolInstallerReference
{
    public int PhaseIndex { get; set; }
    public string PhaseName { get; set; } = string.Empty;
    public int StepIndex { get; set; }
    public string StepDisplayName { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string TaskVersionSpec { get; set; } = string.Empty;
    public string NuGetVersionSpec { get; set; } = string.Empty;
    public string CheckLatest { get; set; } = string.Empty;
}
