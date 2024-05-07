
using Benday.AzureDevOpsUtil.Api.JsonBuilds;
using Benday.AzureDevOpsUtil.Api.Messages;

using Benday.CommandsFramework;

using System.Text;

namespace Benday.AzureDevOpsUtil.Api;


[Command(
    Category = Constants.Category_Builds,
    Name = Constants.CommandArgumentNameImportBuildDefinition,
        Description = "Import a json-based build definition",
        IsAsync = true)]
public class ImportBuildDefinitionCommand : AzureDevOpsCommandBase
{
    private string _TeamProjectName = string.Empty;
    private string _BuildDefinitionName = string.Empty;
    private string _Filename = string.Empty;

    public BuildDefinitionInfoResponse? LastResult { get; private set; }

    public ImportBuildDefinitionCommand(
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

        arguments.AddString(Constants.ArgumentNameFilename)
            .WithDescription("Path to json build file");

        return arguments;
    }

    protected override async Task OnExecute()
    {
        // XamlBuildRunInfo
        _TeamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        _BuildDefinitionName = Arguments.GetStringValue(Constants.ArgumentNameBuildDefinitionName);

        var originalValue = Arguments.GetStringValue(Constants.ArgumentNameFilename);

        WriteLine(originalValue);

        _Filename =
            CommandFrameworkUtilities.GetPathToSourceFile(originalValue, true);


        string json = File.ReadAllText(_Filename);

        // read lines from string
        string[] lines = json.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

        JsonBuildDefinition buildDef =
            System.Text.Json.JsonSerializer.Deserialize<JsonBuildDefinition>(json) ?? 
                throw new KnownException($"Problem reading json file '{_Filename}'");

        var buildId = await GetBuildIdByBuildName(_BuildDefinitionName);

        if (buildId != null)
        {
            buildDef.Name = _BuildDefinitionName;
            buildDef.Id = int.Parse(buildId);

            WriteLine($"Build definition '{_BuildDefinitionName}' already exists as build definition id '{buildId}'.");

            // PUT https://dev.azure.com/{organization}/{project}/_apis/build/definitions/{definitionId}?api-version=7.0

            var requestUrl = $"{_TeamProjectName}/_apis/build/definitions/{buildId}?api-version=7.0";

            var result = await SendPutForBodyAndGetTypedResponseSingleAttempt(requestUrl, buildDef);
            
            WriteLine();

            if (result == null)
            {
                WriteLine("** Result was null **");
            }
            else
            {
                WriteLine($"Build definition '{_BuildDefinitionName}' id '{result.Id}' updated.");
            }
        }
        else
        {

            buildDef.Name = _BuildDefinitionName;
            buildDef.Id = 0;

            var requestUrl = $"{_TeamProjectName}/_apis/build/definitions?api-version=7.0";            

            var result = await SendPostForBodyAndGetTypedResponseSingleAttempt<JsonBuildDefinition, JsonBuildDefinition>(
                               requestUrl, buildDef);            

            WriteLine();

            if (result == null)
            {
                WriteLine("** Result was null **");
            }            
            else
            {
                WriteLine($"Build definition '{_BuildDefinitionName}' created as build definition id '{result.Id}'.");
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
}