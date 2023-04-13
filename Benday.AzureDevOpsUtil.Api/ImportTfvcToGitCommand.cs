using System.Text.Json;
using System.Text;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Excel;
using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(Name = Constants.CommandArgumentNameImportTfvcToGit, 
    Description = "Converts a Team Foundation Version Control (TFVC) folder to a Git repository.", 
    IsAsync = true)]
public class ImportTfvcToGitCommand : AzureDevOpsCommandBase
{

    public ImportTfvcToGitCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        AddCommonArguments(args);
        args.AddString(Constants.ArgumentNameTeamProjectName).AsRequired().
            WithDescription("Team project name that contains the TFVC and Git repositories");
        args.AddString(Constants.ArgumentNameRepositoryName).AsRequired().
            WithDescription("Name of the new git repository");
        args.AddString(Constants.ArgumentNameTfvcFolder).AsRequired().
            WithDescription("Source TFVC folder to convert");

        return args;
    }


    protected override async Task OnExecute()
    {
        var projectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        var repoName = Arguments.GetStringValue(Constants.ArgumentNameRepositoryName);
        var tfvcPath = Arguments.GetStringValue(Constants.ArgumentNameTfvcFolder);
        
        var project = await GetProject(projectName);

        var tfvcValidationResult = await ValidateImport(project, tfvcPath);

        var gitRepoCreateResult = await ValidateAndCreateGitRepository(project, repoName);

        if (gitRepoCreateResult == null)
        {
            throw new KnownException("ERROR: no result from call to create git repository");
        }
        else
        {
            _OutputProvider.WriteLine("Created git repository.");
            _OutputProvider.WriteLine($"{gitRepoCreateResult.Name} ({gitRepoCreateResult.Id}): {gitRepoCreateResult.WebUrl}");

            await ImportTfvcToGit(project, gitRepoCreateResult, tfvcValidationResult);
        }
    }

    private Task ImportTfvcToGit(
        TeamProjectInfo project, 
        GitRepositoryInfo gitRepoCreateResult, 
        TfvcToGitImportRequest tfvcValidationResult)
    {
        var requestUrl =
            $"{project.Id}/_apis/git/repositories/{gitRepoCreateResult.Id}/importRequests?api-version=7.0";

        var body = new TfvcToGitImportExecuteRequest();
        body.DeleteServiceEndpointAfterImportIsDone = true;
        body.TfvcSource.Path = tfvcValidationResult.TfvcSource.Path;
        body.TfvcSource.ImportHistory = tfvcValidationResult.TfvcSource.ImportHistory;
        body.TfvcSource.ImportHistoryDurationInDays = body.TfvcSource.ImportHistoryDurationInDays;

        var returnValue = await SendPostForBodyAndGetTypedResponseSingleAttempt<
            TfvcToGitImportExecuteResponse, TfvcToGitImportExecuteRequest> (
            requestUrl, body);
    }

    private async Task<TfvcToGitImportRequest> ValidateImport(TeamProjectInfo project, string tfvcPath)
    {
        var requestUrl = 
            $"{project.Id}/_apis/git/import/ImportRepositoryValidations?api-version=7.0";

        var body = new TfvcToGitImportRequest();

        body.TfvcSource.Path = tfvcPath;
        body.TfvcSource.ImportHistory = true;
        body.TfvcSource.ImportHistoryDurationInDays = 180;

        var returnValue = await SendPostForBodyAndGetTypedResponseSingleAttempt<
            TfvcToGitImportRequest, TfvcToGitImportRequest>(
            requestUrl, body);

        return returnValue;
    }

    private async Task<TeamProjectInfo> GetProject(string projectName)
    {
        var getProjectArgs = ExecutionInfo.GetCloneOfArguments(
                    Constants.CommandName_GetProject, true);
        var getProject = new GetTeamProjectCommand(getProjectArgs, _OutputProvider);

        await getProject.ExecuteAsync();

        TeamProjectInfo? project = getProject.LastResult;

        if (project == null)
        {
            throw new KnownException($"No project found with name '{projectName}'");
        }
        else
        {
            return project;
        }
    }

    private async Task<GitRepositoryInfo> ValidateAndCreateGitRepository(
        TeamProjectInfo project, string repoName)
    {
        var listGitReposArgs = ExecutionInfo.GetCloneOfArguments(
            Constants.CommandArgumentName_ListGitRepos, true);

        listGitReposArgs.AddArgumentValue(Constants.ArgumentNameRepositoryName, repoName);
        var getExistingRepo = new ListGitRepositoriesForProjectCommand(
            listGitReposArgs, _OutputProvider);

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
            return await CreateGitRepository(project, repoName);
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
            $"{HttpUtility.UrlEncode(project.Id)}/_apis/git/repositories?api-version=7.0";

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
