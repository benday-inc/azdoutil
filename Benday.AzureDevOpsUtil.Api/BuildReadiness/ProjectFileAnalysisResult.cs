namespace Benday.AzureDevOpsUtil.Api.BuildReadiness;

public class ProjectFileAnalysisResult
{
    public string Path { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ProjectType { get; set; } = string.Empty;
    public string NuGetManagementStyle { get; set; } = "None";
    public List<NuGetPackageReference> PackageReferences { get; set; } = new();
    public List<ExternalReference> ExternalReferences { get; set; } = new();
    public List<string> HardcodedPaths { get; set; } = new();
    public List<string> TargetFrameworks { get; set; } = new();
}
