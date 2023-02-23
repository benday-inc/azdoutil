using System.Text.Json;
using System.Text;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Excel;
using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(Name = Constants.CommandArgumentNameCreateGitRepository, Description = "Creates a Git repository in an Azure DevOps Team Project.", IsAsync = true)]
public class CreateGitRepositoryCommand : AzureDevOpsCommandBase
{

    public CreateGitRepositoryCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();


        AddCommonArguments(args);
        args.AddString(Constants.ArgumentNameTeamProjectName).AsRequired().
            WithDescription("Team project name that contains the git repositories");
        args.AddString(Constants.ArgumentNameRepositoryName).AsRequired().
            WithDescription("Name of the new git repository");

        return args;
    }


    protected override async Task OnExecute()
    {
        var projectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        var repoName = Arguments.GetStringValue(Constants.ArgumentNameRepositoryName);

        var result = await CreateGitRepository(projectName, repoName);

        if (result == null)
        {
            _OutputProvider.WriteLine("Result is null");
        }
        else
        {
            _OutputProvider.WriteLine("Created git repository.");
            _OutputProvider.WriteLine($"{result.Name} ({result.Id}): {result.WebUrl}");
        }
    }

    private async Task<GitRepositoryInfo> CreateGitRepository(string projectName, string repoName)
    {
        var getProjectArgs = ExecutionInfo.GetCloneOfArguments(
            Constants.CommandName_GetProject, true);
        var getProject = new GetTeamProjectCommand(getProjectArgs, _OutputProvider);

        await getProject.ExecuteAsync();

        var project = getProject.LastResult;

        if (project == null)
        {
            throw new KnownException($"No project found with name '{projectName}'");
        }

        var listGitReposArgs = ExecutionInfo.GetCloneOfArguments(
            Constants.CommandArgumentName_ListGitRepos, true);

        listGitReposArgs.AddArgumentValue(Constants.ArgumentNameRepositoryName, repoName);
        var getExistingRepo = new ListGitRepositoriesForProjectCommand(getProjectArgs, _OutputProvider);

        await getExistingRepo.ExecuteAsync();

        bool gitRepoExists = true;

        if (getExistingRepo.LastResult == null || getExistingRepo.LastResult.Length == 0)
        {
            // does not exist
            gitRepoExists = false;
        }
        else
        {
            var repositories = getExistingRepo.LastResult;

            var match = repositories.Where(
                x =>
                string.Equals(x.Name, repoName, StringComparison.CurrentCultureIgnoreCase)
                ).FirstOrDefault();

            if (match == null)
            {
                gitRepoExists = false;
            }
        }      

        if (gitRepoExists == true)
        {
            throw new KnownException($"Git repository named '{repoName}' already exists.");
        }
        else
        {
            return await CreateGitRepository(getProject.LastResult!, repoName);
        }
    }

    private async Task<GitRepositoryInfo> CreateGitRepository(TeamProjectInfo project, string repoName)
    {
        var createRequest = new GitRepositoryCreateRequest();

        createRequest.Name = repoName;
        createRequest.Project = project;

        var requestAsJson = JsonSerializer.Serialize<GitRepositoryCreateRequest>(createRequest);

        var requestContent = new StringContent(
            requestAsJson,
            Encoding.UTF8, "application/json");

        var queryString = 
            $"{HttpUtility.UrlEncode(project.Name)}/_apis/git/repositories?api-version=7.0";

        using var client = GetHttpClientInstanceForAzureDevOps();

        var result = await client.PostAsync(queryString, requestContent);

        if (result.IsSuccessStatusCode == false)
        {
            throw new InvalidOperationException($"Problem creating git repo. {result.StatusCode} {result.ReasonPhrase}");
        }

        var responseContent = await result.Content.ReadAsStringAsync();

        var returnValue = JsonSerializer.Deserialize<GitRepositoryInfo>(responseContent);

        return returnValue!;
    }
}
