using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.BuildReadiness;
using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.VersionControl;

[Command(
    Category = Constants.Category_VersionControl,
    Name = Constants.CommandName_AnalyzeRepo,
    IsAsync = true,
    Description = "Analyzes a Git repository for build readiness without cloning.")]
public class AnalyzeRepoCommand : AzureDevOpsCommandBase
{
    public RepositoryAnalysisResult? LastResult { get; private set; }

    public AnalyzeRepoCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        AddCommonArguments(args);

        args.AddString(Constants.ArgumentNameTeamProjectName)
            .AsRequired()
            .WithDescription("Team project name");

        args.AddString(Constants.ArgumentNameRepositoryName)
            .AsRequired()
            .WithDescription("Repository name");

        args.AddBoolean(Constants.ArgumentNameOutputCsv)
            .AllowEmptyValue()
            .AsNotRequired()
            .WithDescription("Output results in CSV format");

        return args;
    }

    protected override async Task OnExecute()
    {
        var projectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        var repoName = Arguments.GetStringValue(Constants.ArgumentNameRepositoryName);
        var outputCsv = Arguments.GetBooleanValue(Constants.ArgumentNameOutputCsv);

        var repo = await FindRepository(projectName, repoName);

        if (repo == null)
        {
            throw new KnownException(
                $"Repository '{repoName}' not found in project '{projectName}'.");
        }

        var result = await AnalyzeRepository(projectName, repo);

        LastResult = result;

        if (!IsQuietMode)
        {
            if (outputCsv)
            {
                WriteCsvOutput(result);
            }
            else
            {
                var formatter = new BuildReadinessReportFormatter();
                WriteLine(formatter.FormatReport(result));
            }
        }
    }

    public async Task<RepositoryAnalysisResult> AnalyzeRepository(
        string projectName, GitRepositoryInfo repo)
    {
        List<string> fileTree;

        try
        {
            fileTree = await GetFileTree(projectName, repo.Id);
        }
        catch
        {
            return new RepositoryAnalysisResult
            {
                ProjectName = projectName,
                RepositoryName = repo.Name,
                RepositoryId = repo.Id,
                HasError = true,
                ErrorMessage = "Failed to retrieve file tree. Repository may be empty or inaccessible."
            };
        }

        var contentProvider = CreateFileContentProvider(projectName, repo.Id);
        var analyzer = new BuildReadinessAnalyzer(fileTree, contentProvider);

        return await analyzer.AnalyzeAsync(projectName, repo.Name, repo.Id);
    }

    private async Task<GitRepositoryInfo?> FindRepository(string projectName, string repoName)
    {
        var listGitReposCommand = new ListGitRepositoriesForProjectCommand(
            ExecutionInfo.GetCloneOfArguments(
                Constants.CommandArgumentName_ListGitRepos, true),
            _OutputProvider);

        var repos = await listGitReposCommand.GetGitRepositories(projectName);

        return repos?.FirstOrDefault(r =>
            string.Equals(r.Name, repoName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<string>> GetFileTree(string projectName, string repositoryId)
    {
        using var client = GetHttpClientInstanceForAzureDevOps();

        var requestUrl =
            $"{projectName}/_apis/git/repositories/{repositoryId}/items?recursionLevel=Full&api-version=7.0";

        var response = await client.GetAsync(requestUrl);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Failed to get file tree: {response.StatusCode} {response.ReasonPhrase}");
        }

        var content = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<GitItemsListResponse>(content);

        if (result?.Value == null)
        {
            return new List<string>();
        }

        return result.Value
            .Where(item => !item.IsFolder)
            .Select(item => item.Path)
            .ToList();
    }

    private IFileContentProvider CreateFileContentProvider(string projectName, string repositoryId)
    {
        return new DelegateFileContentProvider(async (filePath) =>
        {
            using var client = GetHttpClientInstanceForAzureDevOps();

            var requestUrl =
                $"{projectName}/_apis/git/repositories/{repositoryId}/items?path={Uri.EscapeDataString(filePath)}&api-version=7.0";

            var response = await client.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadAsStringAsync();
        });
    }

    private void WriteCsvOutput(RepositoryAnalysisResult result)
    {
        var csvWriter = new Benday.CommandsFramework.DataFormatting.CsvWriter();

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

        WriteLine(csvWriter.ToCsvString());
    }
}
