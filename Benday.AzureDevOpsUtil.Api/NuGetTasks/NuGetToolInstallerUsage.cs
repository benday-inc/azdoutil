namespace Benday.AzureDevOpsUtil.Api.NuGetTasks;

public class NuGetToolInstallerUsage : NuGetToolInstallerReference
{
    public string ProjectName { get; set; } = string.Empty;
    public int BuildDefinitionId { get; set; }
    public string BuildDefinitionName { get; set; } = string.Empty;
}
