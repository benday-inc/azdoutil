namespace Benday.AzureDevOpsUtil.Api.NuGetTasks;

public class NuGetToolInstallerUpdateResult
{
    public List<NuGetToolInstallerStepChange> Changes { get; set; } = new();
    public int UpdatedStepCount => Changes.Count;
}

public class NuGetToolInstallerStepChange
{
    public string PhaseName { get; set; } = string.Empty;
    public int StepIndex { get; set; }
    public string OldTaskVersionSpec { get; set; } = string.Empty;
    public string NewTaskVersionSpec { get; set; } = string.Empty;
    public string OldNuGetVersionSpec { get; set; } = string.Empty;
    public string NewNuGetVersionSpec { get; set; } = string.Empty;
    public string OldDisplayName { get; set; } = string.Empty;
    public string NewDisplayName { get; set; } = string.Empty;
}
