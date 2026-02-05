namespace Benday.AzureDevOpsUtil.Api.BuildReadiness;

public class SolutionAnalysisResult
{
    public string Path { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string SolutionRoot { get; set; } = string.Empty;
    public List<string> ReferencedProjectPaths { get; set; } = new();
    public List<string> SolutionRootViolations { get; set; } = new();
    public bool HasSolutionRootViolations => SolutionRootViolations.Count > 0;
}
