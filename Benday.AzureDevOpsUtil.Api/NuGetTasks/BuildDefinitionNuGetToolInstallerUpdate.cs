namespace Benday.AzureDevOpsUtil.Api.NuGetTasks;

/// <summary>
/// The NuGet tool installer changes made to one build definition.
/// </summary>
public class BuildDefinitionNuGetToolInstallerUpdate
{
    public string ProjectName { get; set; } = string.Empty;
    public int BuildDefinitionId { get; set; }
    public string BuildDefinitionName { get; set; } = string.Empty;
    public NuGetToolInstallerUpdateResult Result { get; set; } = new();

    /// <summary>
    /// Dry run only. Path to the file holding the build definition JSON before the change.
    /// </summary>
    public string BeforeFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Dry run only. Path to the file holding the build definition JSON after the change.
    /// </summary>
    public string AfterFilePath { get; set; } = string.Empty;
}
