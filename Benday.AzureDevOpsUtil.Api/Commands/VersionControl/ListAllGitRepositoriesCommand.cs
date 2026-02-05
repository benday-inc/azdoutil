using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.Commands.ProjectAdministration;
using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;
using Benday.CommandsFramework.DataFormatting;

namespace Benday.AzureDevOpsUtil.Api.Commands.VersionControl;

[Command(
    Category = Constants.Category_VersionControl,
    Name = Constants.CommandArgumentName_ListAllGitRepos,
    IsAsync = true,
    Description = "Gets list of Git repositories from all Azure DevOps Team Projects.")]
public class ListAllGitRepositoriesCommand : AzureDevOpsCommandBase
{
    public ListAllGitRepositoriesCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        AddCommonArguments(args);

        args.AddBoolean(Constants.ArgumentNameOutputCsv)
            .AllowEmptyValue()
            .WithDescription("Output results in CSV format")
            .AsNotRequired();

        args.AddBoolean(Constants.ArgumentNameShowLastCommitInfo)
            .AllowEmptyValue()
            .WithDescription("Include last commit info for each repository")
            .AsNotRequired();

        return args;
    }

    protected override async Task OnExecute()
    {
        var outputCsv = Arguments.GetBooleanValue(Constants.ArgumentNameOutputCsv);
        var showLastCommit = Arguments.GetBooleanValue(Constants.ArgumentNameShowLastCommitInfo);

        var listProjectsCommand = new ListTeamProjectsCommand(
            ExecutionInfo.GetCloneOfArguments(
                Constants.CommandName_ListProjects, true),
            _OutputProvider);

        await listProjectsCommand.ExecuteAsync();

        var projectsResult = listProjectsCommand.LastResult;

        if (projectsResult == null || projectsResult.Projects.Length == 0)
        {
            WriteLine("No team projects found.");
            return;
        }

        var listGitReposCommand = new ListGitRepositoriesForProjectCommand(
            ExecutionInfo.GetCloneOfArguments(
                Constants.CommandArgumentName_ListGitRepos, true),
            _OutputProvider);

        var totalRepoCount = 0;

        CsvWriter? csvWriter = null;

        if (outputCsv)
        {
            csvWriter = new CsvWriter();

            if (showLastCommit)
            {
                csvWriter.AddColumns(
                    "Project Name",
                    "Repository Name",
                    "Repository Id",
                    "Web URL",
                    "Default Branch",
                    "Is Disabled",
                    "Last Commit Date",
                    "Last Commit Author",
                    "Last Commit Comment");
            }
            else
            {
                csvWriter.AddColumns(
                    "Project Name",
                    "Repository Name",
                    "Repository Id",
                    "Web URL",
                    "Default Branch",
                    "Is Disabled");
            }
        }

        foreach (var project in projectsResult.Projects.OrderBy(p => p.Name))
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
                GitCommitInfo? latestCommit = null;

                if (showLastCommit)
                {
                    latestCommit = await GetLatestCommit(project.Name, repo.Id);
                }

                if (outputCsv && csvWriter != null)
                {
                    if (showLastCommit)
                    {
                        csvWriter.AddRow(
                            project.Name,
                            repo.Name,
                            repo.Id,
                            repo.WebUrl,
                            repo.DefaultBranch,
                            repo.IsDisabled.ToString(),
                            latestCommit?.Committer.Date.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty,
                            latestCommit?.Author.Name ?? string.Empty,
                            latestCommit?.Comment ?? string.Empty);
                    }
                    else
                    {
                        csvWriter.AddRow(
                            project.Name,
                            repo.Name,
                            repo.Id,
                            repo.WebUrl,
                            repo.DefaultBranch,
                            repo.IsDisabled.ToString());
                    }
                }
                else if (!IsQuietMode)
                {
                    if (showLastCommit)
                    {
                        var lastCommitInfo = latestCommit != null
                            ? $" | Last commit: {latestCommit.Committer.Date:yyyy-MM-dd} by {latestCommit.Author.Name}"
                            : " | Last commit: (none)";

                        WriteLine($"    {repo.Name} ({repo.Id}): {repo.WebUrl}{lastCommitInfo}");
                    }
                    else
                    {
                        WriteLine($"    {repo.Name} ({repo.Id}): {repo.WebUrl}");
                    }
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
            WriteLine($"Total repositories: {totalRepoCount}");
        }
    }

    private async Task<GitCommitInfo?> GetLatestCommit(string projectName, string repositoryId)
    {
        try
        {
            using var client = GetHttpClientInstanceForAzureDevOps();

            var requestUrl =
                $"{projectName}/_apis/git/repositories/{repositoryId}/commits?searchCriteria.$top=1&api-version=7.0";

            var response = await client.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<GitCommitListResponse>(content);

            if (result == null || result.Value.Length == 0)
            {
                return null;
            }

            return result.Value[0];
        }
        catch
        {
            return null;
        }
    }
}
