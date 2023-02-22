using System.Text.Json;
using System.Text;
using System.Web;
using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;
using OfficeOpenXml.Utils;

namespace Benday.AzureDevOpsUtil.Api;

[Command(Name = Constants.CommandArgumentNameGetWorkItemStates, 
    Description = "Gets the list of states for a work item type in an Azure DevOps Team Project.", 
    IsAsync = true)]
public class GetWorkItemStatesCommand : AzureDevOpsCommandBase
{

    public GetWorkItemStatesCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public GetWorkItemTypeStatesResponse LastResult { get; private set; }

    protected override ArgumentCollection GetAvailableArguments()
    {
        var args = new ArgumentCollection();


        AddCommonArguments(args);
        args.AddString(Constants.ArgumentNameTeamProjectName).AsRequired().
            WithDescription("Team project name that contains the work item type");
        args.AddString(Constants.ArgumentNameWorkItemTypeName).AsRequired().
            WithDescription("Name of the work item type");

        return args;
    }


    protected override async Task OnExecute()
    {
        var projectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        var workItemTypeName = Arguments.GetStringValue(Constants.ArgumentNameWorkItemTypeName);

        var result = await GetWorkItemTypeStates(projectName, workItemTypeName);

        LastResult = result;

        if (IsQuietMode == false)
        {
            if (result == null)
            {
                _OutputProvider.WriteLine("Result is null");
            }
            else
            {
                foreach (var item in LastResult.States)
                {
                    WriteLine($"{item.Name}");
                }
            }
        }
    }

    private async Task<GetWorkItemTypeStatesResponse> GetWorkItemTypeStates(string projectName, string workItemTypeName)
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

        var results = await GetWorkItemTypeStates(project, workItemTypeName);

        return results;
    }

    private async Task<GetWorkItemTypeStatesResponse> GetWorkItemTypeStates(TeamProjectInfo project, string workItemTypeName)
    {
        var createRequest = new GitRepositoryCreateRequest();

        createRequest.Name = workItemTypeName;
        createRequest.Project = project;

        var requestAsJson = JsonSerializer.Serialize<GitRepositoryCreateRequest>(createRequest);

        var queryString =
            $"{project.Name.Replace(" ", "%20")}/_apis/wit/workitemtypes/{workItemTypeName.Replace(" ", "%20")}/states?api-version=7.0";

        using var client = GetHttpClientInstanceForAzureDevOps();

        var result = await client.GetAsync(queryString);

        if (result.IsSuccessStatusCode == false)
        {
            throw new InvalidOperationException($"Problem getting work item state info. {result.StatusCode} {result.ReasonPhrase}");
        }

        var responseContent = await result.Content.ReadAsStringAsync();

        var returnValue = JsonSerializer.Deserialize<GetWorkItemTypeStatesResponse>(responseContent);

        return returnValue!;
    }
}


