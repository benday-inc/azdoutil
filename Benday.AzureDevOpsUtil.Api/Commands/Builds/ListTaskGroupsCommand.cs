using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.Messages.TaskGroups;
using Benday.AzureDevOpsUtil.Api.TaskGroups;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.Builds;

[Command(
    Category = Constants.Category_Builds,
    Name = Constants.CommandName_ListTaskGroups,
    Description = "List task groups in a team project.")]
public class ListTaskGroupsCommand : AzureDevOpsCommandBase
{
    public List<TaskGroupInfo>? LastResult { get; private set; }

    public ListTaskGroupsCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);

        arguments.AddString(Constants.ArgumentNameTeamProjectName)
            .AsRequired()
            .WithDescription("Team project name");

        arguments.AddBoolean(Constants.ArgumentNameNameOnly)
            .AllowEmptyValue()
            .AsNotRequired()
            .WithDescription("Only display the task group name");

        arguments.AddBoolean(Constants.CommandArgumentNameToJson)
            .AllowEmptyValue()
            .AsNotRequired()
            .WithDescription("Output results as JSON");

        return arguments;
    }

    public async Task<List<TaskGroupInfo>> GetTaskGroups(string teamProjectName)
    {
        using var http = GetHttpClientInstanceForAzureDevOps();
        var client = new TaskGroupClient(http);
        var results = await client.ListAsync(teamProjectName);

        LastResult = results;

        return results;
    }

    protected override async Task OnExecute(CancellationToken cancellationToken)
    {
        var teamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        var nameOnly = Arguments.GetBooleanValue(Constants.ArgumentNameNameOnly);
        var toJson = Arguments.GetBooleanValue(Constants.CommandArgumentNameToJson);

        var results = await GetTaskGroups(teamProjectName);

        if (IsQuietMode == true)
        {
            return;
        }

        if (toJson == true)
        {
            WriteLine(JsonSerializer.Serialize(results, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
            return;
        }

        WriteLine();
        WriteLine($"Result count: {results.Count}");

        var ordered = results.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var item in ordered)
        {
            WriteLine(Format(item, nameOnly));
        }
    }

    private static string Format(TaskGroupInfo item, bool nameOnly)
    {
        if (nameOnly)
        {
            return item.Name;
        }

        return $"{item.Name} (id: {item.Id}, version: {item.Version}, revision: {item.Revision})";
    }
}
