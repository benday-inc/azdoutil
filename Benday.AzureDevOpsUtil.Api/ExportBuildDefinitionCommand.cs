using System.Net;
using System.Text;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(Name = Constants.CommandArgumentNameExportBuildDefinition,
        Description = "Export build definition",
        IsAsync = true)]
public class ExportBuildDefinitionCommand : AzureDevOpsCommandBase
{
    private string _TeamProjectName = string.Empty;
    private string _BuildDefinitionName = string.Empty;

    public BuildDefinitionInfoResponse? LastResult { get; private set; }

    public ExportBuildDefinitionCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        arguments.AddString(Constants.ArgumentNameTeamProjectName)
            .WithDescription("Team project name");

        arguments.AddString(Constants.ArgumentNameBuildDefinitionName)
            .WithDescription("Build definition name");

        arguments.AddBoolean(Constants.ArgumentNameXaml)
            .AllowEmptyValue()
            .WithDescription("List XAML build definitions")
            .AsNotRequired();

        return arguments;
    }

    protected override async Task OnExecute()
    {
        _TeamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        _BuildDefinitionName = Arguments.GetStringValue(Constants.ArgumentNameBuildDefinitionName);
        var isXamlMode = Arguments.GetBooleanValue(Constants.ArgumentNameXaml);

        var buildId = await GetBuildIdByBuildName(_BuildDefinitionName);

        if (buildId == null)
        {
                throw new KnownException(
                    String.Format("Build name '{0}' was not found.", _BuildDefinitionName));         
        }
        else
        {
            var apiVersion = "7.0";

            if (isXamlMode == true)
            {
                apiVersion = "2.0";
            }

            var requestUrl = $"{_TeamProjectName}/_apis/build/definitions/{buildId}?api-version={apiVersion}";

            var json = await GetStringAsync(requestUrl);

            WriteLine();

            if (json == null)
            {
                WriteLine("** Result was null **");
            }
            else
            {
                WriteLine(json);
            }
        }
    }

    private async Task<string?> GetBuildIdByBuildName(string buildDefinitionName)
    {
        var result = await GetBuildDefinitionByName(buildDefinitionName);

        if (result == null)
        {
            return null;
        }
        else
        {
            return result.Id.ToString();
        }
    }

    private async Task<BuildDefinitionInfo?> GetBuildDefinitionByName(string name)
    {
        string requestUrl;

        if (Arguments.GetBooleanValue(Constants.ArgumentNameXaml) == true)
        {
            requestUrl = $"{_TeamProjectName}/_apis/build/definitions?api-version=2.2&name={name}";
        }
        else
        {
            requestUrl = $"{_TeamProjectName}/_apis/build/definitions?api-version=7.0&name={name}";
        }

        var result = await CallEndpointViaGetAndGetResult<BuildDefinitionInfoResponse>(requestUrl);

        if (result == null || result.Count == 0 || result.Values.Count == 0)
        {
            return null;
        }
        else
        {
            return result.Values[0];
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
            requestUrl = $"{_TeamProjectName}/_apis/build/definitions?api-version=7.0";
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
        }

        return builder.ToString();
    }
}
