using System.Net;
using System.Text;
using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.AzureDevOpsUtil.Api.Messages.AgentPools;
using Benday.AzureDevOpsUtil.Api.Messages.BuildQueues;
using Benday.CommandsFramework;

using Benday.AzureDevOpsUtil.Api.Commands.ProjectAdministration;
namespace Benday.AzureDevOpsUtil.Api.Commands.Builds;

[Command(
    Category = Constants.Category_Builds,
    Name = Constants.CommandArgumentNameListQueues,
        Description = "List build queues in a team project or team projects",
        IsAsync = true)]
public class ListQueuesCommand : AzureDevOpsCommandBase
{
    public GetBuildQueuesResponse? LastResult { get; private set; }

    public ListQueuesCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);

        arguments.AddString(Constants.ArgumentNameTeamProjectName)
            .WithDescription("Team project name").
            AsNotRequired();

        arguments.AddBoolean(Constants.ArgumentNameAllProjects)
            .AllowEmptyValue()
            .WithDescription("All builds in all projects in this collection")
            .AsNotRequired();

        arguments.AddBoolean(Constants.CommandArgumentNameToJson).
            WithDescription("Output as JSON").WithDefaultValue(false).AllowEmptyValue().AsNotRequired();

        return arguments;
    }

    private async Task<GetBuildQueuesResponse?> GetBuildQueues(string teamProjectName)
    {
        var requestUrl = $"{teamProjectName}/_apis/distributedtask/queues?api-version=7.1";

        var result = await CallEndpointViaGetAndGetResult<GetBuildQueuesResponse>(requestUrl);

        return result;
    }

    private string _TeamProjectName = string.Empty;

    private async Task<List<BuildQueueInfo>> GetBuildQueuesForAllProjects()
    {
        List<BuildQueueInfo> returnValue = new();

        // call ListTeamProjectsCommand
        var command = new ListTeamProjectsCommand(
            ExecutionInfo.GetCloneOfArguments(
               Constants.CommandName_ListProjects, true), _OutputProvider);

        await command.ExecuteAsync();

        if (command.LastResult == null)
        {
            throw new KnownException("No team projects found.");
        }
        else
        {
            var teamProjects = command.LastResult.Projects;

            foreach (var teamProject in teamProjects)
            {
                var result = await GetBuildQueuesForProject(teamProject.Name);

                if (result != null)
                {
                    returnValue.AddRange(result.Value);
                }
            }
        }

        return returnValue;
    }

    private async Task<GetBuildQueuesResponse?> GetBuildQueuesForProject(string teamProjectName)
    {
        var result = await GetBuildQueues(teamProjectName);

        if (result == null || result.Count == 0)
        {
            WriteLine($"No build queues found for team project '{teamProjectName}'.");
            return null;
        }
        else
        {
            foreach (var item in result.Value)
            {
                item.TeamProjectName = teamProjectName;
            }

            return result;
        }
    }

    private void Print(BuildQueueInfo item)
    {
        WriteLine("***********");
        WriteLine("Id", item.Id);
        WriteLine("ProjectId", item.ProjectId);
        WriteLine("TeamProjectName", item.TeamProjectName);
        WriteLine("Name", item.Name);
        WriteLine("Pool.Id", item.Pool.Id);
        WriteLine("Pool.Scope", item.Pool.Scope);
        WriteLine("Pool.Name", item.Pool.Name);
        WriteLine("Pool.IsHosted", item.Pool.IsHosted);
        WriteLine("Pool.PoolType", item.Pool.PoolType);
        WriteLine("Pool.Size", item.Pool.Size);
        WriteLine("Pool.IsLegacy", item.Pool.IsLegacy);
        WriteLine("Pool.Options", item.Pool.Options);
        WriteLine("***********");
        WriteLine();
    }

    protected override async Task OnExecute()
    {
        var toJson = Arguments.GetBooleanValue(Constants.CommandArgumentNameToJson);

        if (Arguments.HasValue(Constants.ArgumentNameAllProjects) == false &&
            Arguments.HasValue(Constants.ArgumentNameTeamProjectName) == false)
        {
            throw new KnownException(
                $"You must specify either /{Constants.ArgumentNameAllProjects} or supply a value for /{Constants.ArgumentNameTeamProjectName}.");
        }
        else if (Arguments.HasValue(Constants.ArgumentNameAllProjects) == true &&
            Arguments.HasValue(Constants.ArgumentNameTeamProjectName) == true)
        {
            throw new KnownException(
                $"You cannot specify both /{Constants.ArgumentNameAllProjects} and /{Constants.ArgumentNameTeamProjectName} at the same time.");
        }

        bool allProjects;

        if (Arguments.GetBooleanValue(Constants.ArgumentNameAllProjects) == true)
        {
            _TeamProjectName = string.Empty;
            allProjects = true;
        }
        else
        {
            _TeamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
            allProjects = false;
        }

        var values = new List<BuildQueueInfo>();

        if (allProjects == false)
        {
            var result = await GetBuildQueuesForProject(_TeamProjectName);

            if (result !=null)
            {
                values.AddRange(result.Value);
            }
        }
        else
        {
            values = await GetBuildQueuesForAllProjects();
        }

        if (toJson == false)
        {
            WriteLine($"Result count: {values.Count}");
            foreach (var item in values)
            {
                Print(item);
            }
        }
        else
        {
            var json = JsonSerializer.Serialize(values, 
                new JsonSerializerOptions { WriteIndented = true });
            WriteLine(json);
        }
    }
    private void WriteLine(string label, string? value)
    {
        WriteLine($"{label}: {value}");
    }

    private void WriteLine(string label, bool value)
    {
        WriteLine(label, value.ToString());
    }

    private void WriteLine(string label, int value)
    {
        WriteLine(label, value.ToString());
    }
}
