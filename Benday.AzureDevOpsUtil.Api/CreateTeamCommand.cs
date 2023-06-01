using System.Text.Json;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(Name = Constants.CommandArgumentName_CreateTeam,
    IsAsync = true,
    Description = "Creates a new team in an Azure DevOps Team Project.")]
public class CreateTeamCommand : AzureDevOpsCommandBase
{
    public TeamInfo? LastResult { get; private set; }

    public CreateTeamCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        AddCommonArguments(args);
        args.AddString(Constants.ArgumentNameTeamProjectName).AsRequired().
            WithDescription("Team project name that contains the team");
        args.AddString(Constants.ArgumentNameTeamName).AsRequired().
            WithDescription("Name of the new team");

        args.AddString(Constants.ArgumentNameTeamDescription).AsNotRequired().
            WithDescription("Description for the new team");

        return args;
    }

    protected override async Task OnExecute()
    {
        var projectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        var teamName = Arguments.GetStringValue(Constants.ArgumentNameTeamName);
        var description = string.Empty;

        if (Arguments.HasValue(Constants.ArgumentNameTeamDescription) == true)
        {
            description = Arguments.GetStringValue(Constants.ArgumentNameTeamDescription);
        }

        var project = await GetTeamProject(projectName);
        var connectionData = await GetConnectionData();

        var result = await GetTeams(project.Id);

        var match = result.Where(x => string.Compare(x.Name, teamName, true) == 0).FirstOrDefault();

        if (match != null)
        {
            throw new KnownException($"Team '{teamName}' already exists.");
        }

        var requestData = new CreateTeamRequest()
        {
            Name = teamName,
            Description = description
        };

        requestData.AddUser(connectionData.AuthenticatedUser.Id);

        /*
        var response = await SendPostForBodyAndGetTypedResponseSingleAttempt<TeamInfo, CreateTeamRequest>(
                       $"_apis/projects/{project.Id}/teams?api-version=7.0",
                       requestData);
        */

        var response = await SendPostForBodyAndGetTypedResponseSingleAttempt<TeamInfo, CreateTeamRequest>(
                       $"{project.Id}/_api/_identity/CreateTeam?__v=5",
                       requestData);

        LastResult = response;

        if (IsQuietMode == false)
        {
            if (response.Id == project.DefaultTeam?.Id)
            {
                WriteLine($"{response.Name} ({response.Id}, Default Team) -- {response.Description}");
            }
            else
            {
                WriteLine($"{response.Name} ({response.Id}) -- {response.Description}");
            }
        }
    }

    private async Task<TeamProjectInfo> GetTeamProject(string teamProjectName)
    {
        var args = ExecutionInfo.GetCloneOfArguments(
                        Constants.CommandName_GetProject,
                        true);

        args.AddArgumentValue(Constants.ArgumentNameTeamProjectName, teamProjectName);

        var command = new GetTeamProjectCommand(
            args, _OutputProvider);

        await command.ExecuteAsync();

        if (command.LastResult == null)
        {
            throw new KnownException($"Could not team project '{teamProjectName}' for work item.");
        }

        return command.LastResult;
    }

    private async Task<ConnectionDataResponse> GetConnectionData()
    {
        var args = ExecutionInfo.GetCloneOfArguments(
                        Constants.CommandName_ConnectionData,
                        true);

        var command = new GetConnectionDataCommand(
            args, _OutputProvider);

        await command.ExecuteAsync();

        if (command.LastResult == null)
        {
            throw new KnownException($"Could not get connection data.");
        }

        return command.LastResult;
    }

    public async Task<TeamInfo[]> GetTeams(string projectId)
    {
        using var client = GetHttpClientInstanceForAzureDevOps();

        var results = await client.GetAsync($"_apis/projects/{projectId}/teams");

        if (results.IsSuccessStatusCode == false)
        {
            throw new InvalidOperationException($"Request failed -- {results.StatusCode} {results.ReasonPhrase}");
        }

        var content = await results.Content.ReadAsStringAsync();

        var objectResults = JsonSerializer.Deserialize<GetTeamsResponse>(content);

        if (objectResults == null)
        {
            return new TeamInfo[] { };
        }

        return objectResults.Teams;
    }
}
