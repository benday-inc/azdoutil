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

        arguments.AddString(Constants.ArgumentNameTeamProjectName)
            .WithDescription("Team project name");

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
        _TeamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);

        var toJson = Arguments.GetBooleanValue(Constants.CommandArgumentNameToJson);

        var results = await GetResult();

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

    private async Task<List<BuildDefinitionInfo>?> GetResult()
    {
        string requestUrl;

        if (Arguments.GetBooleanValue(Constants.ArgumentNameXaml) == true)
        {
            WriteLine("** GETTING XAML BUILD DEFINITIONS **");
            requestUrl = $"{_TeamProjectName}/_apis/build/definitions?api-version=2.2";
        }
        else
        {
            requestUrl = $"{_TeamProjectName}/_apis/build/definitions?api-version=7.1";
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
