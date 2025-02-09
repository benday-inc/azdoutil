using System.Net;
using System.Text;
using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(
    Category = Constants.Category_Builds,
    Name = Constants.CommandArgumentNameListBuildDefinitions,
        Description = "List build definitions",
        IsAsync = true)]
public class ListBuildDefinitionsCommand : AzureDevOpsCommandBase
{
    private string _TeamProjectName = string.Empty;

    public BuildDefinitionInfoResponse? LastResult { get; private set; }

    public ListBuildDefinitionsCommand(
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

        arguments.AddBoolean(Constants.ArgumentNameNameOnly)
            .AllowEmptyValue()
            .WithDescription("Only display the build definition name")
            .AsNotRequired();

        arguments.AddBoolean(Constants.ArgumentNameXaml)
            .AllowEmptyValue()
            .WithDescription("List XAML build definitions")
            .AsNotRequired();

        arguments.AddBoolean(Constants.CommandArgumentNameToJson)
            .AllowEmptyValue()
            .WithDescription("Export to JSON")
            .AsNotRequired();

        return arguments;
    }

    private string SerializeObjectToJson(List<BuildDefinitionInfo> results)
    {
        var json = JsonSerializer.Serialize(results, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return json;
    }

    protected override async Task OnExecute()
    {
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

        var toJson = Arguments.GetBooleanValue(Constants.CommandArgumentNameToJson);

        if (allProjects == false)
        {
            await ListForSingleProject(toJson, _TeamProjectName);
        }
        else
        {
            await ListForAllProjects(toJson);
        }
    }
    private async Task ListForAllProjects(bool json)
    {
        // call ListTeamProjectsCommand
        var command = new ListTeamProjectsCommand(
            ExecutionInfo.GetCloneOfArguments(
               Constants.CommandName_ListProcessTemplates, true), _OutputProvider);

        await command.ExecuteAsync();

        if (command.LastResult == null)
        {
            throw new KnownException("No team projects found.");
        }
        else
        {
            var teamProjects = command.LastResult.Projects;

            var results = new List<BuildDefinitionInfo>();

            foreach (var teamProject in teamProjects)
            {
                var result = await GetResult(teamProject.Name);

                if (result != null && result.Count > 0)
                {
                    results.AddRange(result);
                }
            }

            if (json == true)
            {
                WriteLine(SerializeObjectToJson(results));
            }
            else
            {
                WriteLine(String.Empty);
                WriteLine($"Result count: {results.Count}");

                var groupedResults = results.GroupBy(x => x.Project.Name);

                // order by team project name
                groupedResults = groupedResults.OrderBy(x => x.Key);

                foreach (var groupedResult in groupedResults)
                {
                    WriteLine();
                    WriteLine("** Team Project: " + groupedResult.Key);

                    foreach (var item in groupedResult)
                    {
                        WriteLine(ToString(item));
                    }
                }
            }
        }

    }

    private async Task ListForSingleProject(bool toJson, string teamProjectName)
    {
        var results = await GetResult(teamProjectName);

        if (results == null)
        {
            WriteLine(String.Empty);
            WriteLine("No build definitions found");
        }
        else if (toJson == true)
        {
            WriteLine(SerializeObjectToJson(results));
        }
        else
        {
            WriteLine(String.Empty);

            WriteLine($"Result count: {results.Count}");

            results.ForEach(x => WriteLine(ToString(x)));
        }
    }

    private async Task<List<BuildDefinitionInfo>?> GetResult(string teamProjectName)
    {
        string requestUrl;

        if (Arguments.GetBooleanValue(Constants.ArgumentNameXaml) == true)
        {
            WriteLine("** GETTING XAML BUILD DEFINITIONS **");
            requestUrl = $"{teamProjectName}/_apis/build/definitions?api-version=2.2";
        }
        else
        {
            requestUrl = $"{teamProjectName}/_apis/build/definitions?api-version=7.1";
        }

        var result = await CallEndpointViaGetAndGetResult<BuildDefinitionInfoResponse>(requestUrl);

        LastResult = result;

        return result?.Values;
    }

    private string ToString(BuildDefinitionInfo definition)
    {
        var builder = new StringBuilder();
        builder.Append(definition.Name);

        if (Arguments.GetBooleanValue(Constants.ArgumentNameNameOnly) == false)
        {
            builder.Append(" (");
            builder.Append(definition.Id);
            builder.Append(")");      
            
            if (definition.Queue != null && definition.Queue.Pool != null)
            {
                builder.Append(" - [Queue Name: ");
                builder.Append(definition.Queue.Pool.Name);
                builder.Append(", Queue Pool Id: ");
                builder.Append(definition.Queue.Pool.Id);
                builder.Append(", Pool Is Hosted: ");
                builder.Append(definition.Queue.Pool.IsHosted);
                builder.Append("]");
            }
        }

        return builder.ToString();
    }
}
