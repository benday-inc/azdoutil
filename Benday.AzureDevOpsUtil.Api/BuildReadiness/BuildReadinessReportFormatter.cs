using System.Text;

namespace Benday.AzureDevOpsUtil.Api.BuildReadiness;

public class BuildReadinessReportFormatter
{
    public string FormatReport(RepositoryAnalysisResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine("==========================================================");
        sb.AppendLine($"Build Readiness Report: {result.RepositoryName}");
        sb.AppendLine($"Project: {result.ProjectName}");
        sb.AppendLine("==========================================================");

        if (result.HasError)
        {
            sb.AppendLine();
            sb.AppendLine($"ERROR: {result.ErrorMessage}");
            return sb.ToString();
        }

        FormatSummary(sb, result);
        FormatSolutions(sb, result);
        FormatProjectFiles(sb, result);
        FormatNuGetPackages(sb, result);
        FormatBuildConfigFiles(sb, result);

        return sb.ToString();
    }

    private void FormatSummary(StringBuilder sb, RepositoryAnalysisResult result)
    {
        sb.AppendLine();
        sb.AppendLine("--- Summary ---");
        sb.AppendLine($"  Solutions:                {result.SolutionCount}");
        sb.AppendLine($"  Project files:            {result.ProjectFileCount}");
        sb.AppendLine($"  Has submodules:           {FormatBool(result.HasSubmodules)}");
        sb.AppendLine($"  Has packages.config:      {FormatBool(result.HasPackagesConfig)}");
        sb.AppendLine($"  Has PackageReference:     {FormatBool(result.HasPackageReference)}");
        sb.AppendLine($"  Solution root violations: {FormatBool(result.HasSolutionRootViolations)}");
        sb.AppendLine($"  External references:      {FormatBool(result.HasExternalReferences)}");
        sb.AppendLine($"  Hardcoded paths:          {FormatBool(result.HasHardcodedPaths)}");
        sb.AppendLine($"  Build config files:       {result.BuildConfigFiles.Count}");
        sb.AppendLine($"  NuGet packages:           {result.AllDistinctPackageReferences.Count}");
    }

    private void FormatSolutions(StringBuilder sb, RepositoryAnalysisResult result)
    {
        if (result.Solutions.Count == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine("--- Solutions ---");

        foreach (var solution in result.Solutions)
        {
            sb.AppendLine($"  {solution.Path}");
            sb.AppendLine($"    Solution root: {solution.SolutionRoot}");
            sb.AppendLine($"    Referenced projects: {solution.ReferencedProjectPaths.Count}");

            foreach (var projectPath in solution.ReferencedProjectPaths)
            {
                sb.AppendLine($"      - {projectPath}");
            }

            if (solution.HasSolutionRootViolations)
            {
                sb.AppendLine($"    *** SOLUTION ROOT VIOLATIONS ({solution.SolutionRootViolations.Count}): ***");

                foreach (var violation in solution.SolutionRootViolations)
                {
                    sb.AppendLine($"      - {violation}");
                }
            }
        }
    }

    private void FormatProjectFiles(StringBuilder sb, RepositoryAnalysisResult result)
    {
        if (result.ProjectFiles.Count == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine("--- Project Files ---");

        foreach (var project in result.ProjectFiles)
        {
            sb.AppendLine($"  {project.Path}");
            sb.AppendLine($"    Type: {project.ProjectType}");
            sb.AppendLine($"    NuGet style: {project.NuGetManagementStyle}");

            if (project.TargetFrameworks.Count > 0)
            {
                sb.AppendLine($"    Target frameworks: {string.Join(", ", project.TargetFrameworks)}");
            }

            if (project.PackageReferences.Count > 0)
            {
                sb.AppendLine($"    Package references: {project.PackageReferences.Count}");
            }

            if (project.ExternalReferences.Count > 0)
            {
                sb.AppendLine($"    External references:");

                foreach (var extRef in project.ExternalReferences)
                {
                    sb.AppendLine($"      [{extRef.ReferenceType}] {extRef.Path}");
                }
            }

            if (project.HardcodedPaths.Count > 0)
            {
                sb.AppendLine($"    *** HARDCODED PATHS: ***");

                foreach (var path in project.HardcodedPaths)
                {
                    sb.AppendLine($"      - {path}");
                }
            }
        }
    }

    private void FormatNuGetPackages(StringBuilder sb, RepositoryAnalysisResult result)
    {
        if (result.AllDistinctPackageReferences.Count == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine("--- NuGet Packages ---");

        foreach (var package in result.AllDistinctPackageReferences)
        {
            sb.AppendLine($"  {package.Name} ({package.Version})");
        }
    }

    private void FormatBuildConfigFiles(StringBuilder sb, RepositoryAnalysisResult result)
    {
        if (result.BuildConfigFiles.Count == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine("--- Build Config Files ---");

        foreach (var configFile in result.BuildConfigFiles)
        {
            sb.AppendLine($"  {configFile.Path}");
        }
    }

    private static string FormatBool(bool value)
    {
        return value ? "Yes" : "No";
    }
}
