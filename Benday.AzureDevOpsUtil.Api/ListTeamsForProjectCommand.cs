using System.Text.Json;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(Name = Constants.CommandArgumentName_ListTeams,
    IsAsync = true,
    Description = "Gets list of teams in an Azure DevOps Team Project.")]
public class ListTeamsForProjectCommand : AzureDevOpsCommandBase
{
    public TeamInfo[]? LastResult { get; private set; }

    public ListTeamsForProjectCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        AddCommonArguments(args);
        args.AddString(Constants.ArgumentNameTeamProjectName).AsRequired().
            WithDescription("Team project name that contains the teams");      

        return args;
    }

    protected override async Task OnExecute()
    {
        var projectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);

        var project = await GetTeamProject(projectName);

        var result = await GetTeams(project.Id);

        LastResult = result;

        if (IsQuietMode)
        {
            return;
        }
        else if (result == null)
        {
            WriteLine("Result is null");
        }
        else if (result.Length == 0)
        {
            WriteLine("Result length is 0.");
        }
        else
        {
            WriteLine($"Repository count: {result.Length}");

            foreach (var item in result)
            {
                if (item.Id == project.DefaultTeam?.Id)
                {
                    WriteLine($"{item.Name} ({item.Id}, Default Team) -- {item.Description}");
                }
                else
                {
                    WriteLine($"{item.Name} ({item.Id}) -- {item.Description}");
                }
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
