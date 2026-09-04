using Benday.AzureDevOpsUtil.Api.BuildReadiness;
using Benday.AzureDevOpsUtil.Api.Commands.ProjectAdministration;
using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;
using Benday.CommandsFramework.DataFormatting;

namespace Benday.AzureDevOpsUtil.Api.Commands.VersionControl;

[Command(
    Category = Constants.Category_VersionControl,
    Name = Constants.CommandName_AnalyzeAllRepos,
    Description = "Analyzes all Git repositories for build readiness without cloning.")]
public class AnalyzeAllReposCommand : AzureDevOpsCommandBase
{
    public AnalyzeAllReposCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        AddCommonArguments(args);

        args.AddString(Constants.ArgumentNameTeamProjectName)
            .AsNotRequired()
            .WithDescription("Team project name (if omitted, analyzes all projects)");

        args.AddBoolean(Constants.ArgumentNameOutputCsv)
            .AllowEmptyValue()
            .AsNotRequired()
            .WithDescription("Output results in CSV format");

        return args;
    }

    protected override async Task OnExecute(CancellationToken cancellationToken)
    {
        var outputCsv = Arguments.GetBooleanValue(Constants.ArgumentNameOutputCsv);

        var teamProjectName = string.Empty;

        if (Arguments.HasValue(Constants.ArgumentNameTeamProjectName) == true)
        {
            teamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        }

        var projects = await GetProjects(teamProjectName);

        if (projects == null || projects.Length == 0)
        {
            WriteLine("No team projects found.");
            return;
        }

        // both are created rather than run: each project's repositories are read through
        // GetGitRepositories() and analyzed through AnalyzeRepository(), so neither command
        // ever sees a team project or repository argument of its own
        var analyzeRepoCommand = CreateAzdoCommand<AnalyzeRepoCommand>();

        var listGitReposCommand = CreateAzdoCommand<ListGitRepositoriesForProjectCommand>();

        var allResults = new List<RepositoryAnalysisResult>();
        var formatter = new BuildReadinessReportFormatter();
        CsvWriter? csvWriter = null;

        if (outputCsv)
        {
            csvWriter = new CsvWriter();

            csvWriter.AddColumns(
                "Project Name",
                "Repository Name",
                "Solution Count",
                "Project File Count",
                "Has Submodules",
                "Has PackagesConfig",
                "Has PackageReference",
                "Has Solution Root Violations",
                "Has External References",
                "Has Hardcoded Paths",
                "Build Config Files",
                "NuGet Packages",
                "Error");
        }

        var totalRepoCount = 0;

        foreach (var project in projects.OrderBy(p => p.Name))
        {
            var repos = await listGitReposCommand.GetGitRepositories(project.Name);

            if (repos == null || repos.Length == 0)
            {
                if (!IsQuietMode && !outputCsv)
                {
                    WriteLine($"--- {project.Name}: (no repositories)");
                }

                continue;
            }

            totalRepoCount += repos.Length;

            if (!outputCsv && !IsQuietMode)
            {
                WriteLine($"--- {project.Name}: {repos.Length} repository(ies)");
            }

            foreach (var repo in repos.OrderBy(r => r.Name))
            {
                var result = await analyzeRepoCommand.AnalyzeRepository(project.Name, repo);

                allResults.Add(result);

                if (outputCsv && csvWriter != null)
                {
                    csvWriter.AddRow(
                        result.ProjectName,
                        result.RepositoryName,
                        result.SolutionCount.ToString(),
                        result.ProjectFileCount.ToString(),
                        result.HasSubmodules.ToString(),
                        result.HasPackagesConfig.ToString(),
                        result.HasPackageReference.ToString(),
                        result.HasSolutionRootViolations.ToString(),
                        result.HasExternalReferences.ToString(),
                        result.HasHardcodedPaths.ToString(),
                        result.BuildConfigFiles.Count.ToString(),
                        result.AllDistinctPackageReferences.Count.ToString(),
                        result.HasError ? result.ErrorMessage : string.Empty);
                }
                else if (!IsQuietMode)
                {
                    WriteLine(formatter.FormatReport(result));
                }
            }
        }

        if (outputCsv && csvWriter != null)
        {
            WriteLine(csvWriter.ToCsvString());
        }
        else if (!IsQuietMode)
        {
            WriteLine(string.Empty);
            WriteLine($"Total repositories analyzed: {totalRepoCount}");
        }
    }

    private async Task<TeamProjectInfo[]?> GetProjects(string teamProjectName)
    {
        if (!string.IsNullOrWhiteSpace(teamProjectName))
        {
            var listProjectsCommand = await ExecuteAzdoCommandAsync<ListTeamProjectsCommand>();

            var allProjects = listProjectsCommand.LastResult?.Projects;

            if (allProjects == null)
            {
                return null;
            }

            var match = allProjects.FirstOrDefault(p =>
                string.Equals(p.Name, teamProjectName, StringComparison.OrdinalIgnoreCase));

            return match != null ? new[] { match } : null;
        }
        else
        {
            var listProjectsCommand = await ExecuteAzdoCommandAsync<ListTeamProjectsCommand>();

            return listProjectsCommand.LastResult?.Projects;
        }
    }
}
