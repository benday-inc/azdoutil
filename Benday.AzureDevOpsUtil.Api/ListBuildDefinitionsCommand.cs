using System.Net;
using System.Text;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(Name = Constants.CommandArgumentNameListBuildDefinitions,
        Description = "List build definitions",
        IsAsync = true)]
public class ListBuildDefinitionsCommand : AzureDevOpsCommandBase
{
    private string _TeamProjectName;

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

        return arguments;
    }

    protected override async Task OnExecute()
    {
        _TeamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);

        var results = await GetResult();

        if (results == null)
        {
            WriteLine(String.Empty);
            WriteLine("No build definitions found");
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
        var requestUrl = $"{_TeamProjectName}/_apis/build/definition?api-version=7.0";

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
        }

        return builder.ToString();
    }
}
