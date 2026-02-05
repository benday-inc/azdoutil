namespace Benday.AzureDevOpsUtil.Api.BuildReadiness;

public class RepositoryAnalysisResult
{
    public string ProjectName { get; set; } = string.Empty;
    public string RepositoryName { get; set; } = string.Empty;
    public string RepositoryId { get; set; } = string.Empty;

    public List<SolutionAnalysisResult> Solutions { get; set; } = new();
    public List<ProjectFileAnalysisResult> ProjectFiles { get; set; } = new();
    public List<BuildConfigFileInfo> BuildConfigFiles { get; set; } = new();

    public bool HasSubmodules { get; set; }
    public bool HasPackagesConfig { get; set; }
    public bool HasPackageReference { get; set; }

    public bool HasSolutionRootViolations =>
        Solutions.Any(s => s.HasSolutionRootViolations);

    public bool HasExternalReferences =>
        ProjectFiles.Any(p => p.ExternalReferences.Count > 0);

    public bool HasHardcodedPaths =>
        ProjectFiles.Any(p => p.HardcodedPaths.Count > 0);

    public int SolutionCount => Solutions.Count;
    public int ProjectFileCount => ProjectFiles.Count;

    public List<NuGetPackageReference> AllDistinctPackageReferences { get; set; } = new();

    public bool HasError { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
