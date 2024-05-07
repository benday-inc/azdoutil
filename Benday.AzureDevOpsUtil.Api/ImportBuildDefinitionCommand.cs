
using Benday.AzureDevOpsUtil.Api.BuildUpgraders;
using Benday.AzureDevOpsUtil.Api.JsonBuilds;
using Benday.AzureDevOpsUtil.Api.Messages;

using Benday.CommandsFramework;

using System.Text;
using System.Linq;

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
            .FromPositionalArgument(1)
            .AsRequired()
            .WithDescription("Path to json build file");

        arguments.AddString(Constants.ArgumentNameXamlDumpFilename)
            .AsNotRequired()
            .FromPositionalArgument(2)
            .WithDescription("Path to xaml build info dump file. Use this to upgrade a build using info from an old XAML build.");

        return arguments;
    }

    private void SetXamlInfoToJsonBuild(XamlBuildDumpInfo xamlDumpInfo, JsonBuildDefinition buildDef,
         string teamProjectName, string tpcUrl, string defaultBranch, string rootFolder)
    {
        var fromProjectToBuild = xamlDumpInfo.ProjectsToBuild.FirstOrDefault();
        if (fromProjectToBuild == null)
        {

        }
        else
        {
            var toInput = buildDef.ProcessParameters.Inputs.FirstOrDefault();

            if (toInput == null)
            {
                toInput = new Input();
                buildDef.ProcessParameters.Inputs.Append(toInput);
            }

            toInput.Name = "solution";
            toInput.Label = "Solution";
            toInput.DefaultValue = fromProjectToBuild;
            toInput.Required = true;
            toInput.Type = "filePath";
        }

        SetVariables(xamlDumpInfo, buildDef);
        SetTfvcSourceControl(xamlDumpInfo, buildDef, teamProjectName, tpcUrl, defaultBranch, rootFolder);
    }

    private void SetTfvcSourceControl(XamlBuildDumpInfo xamlDumpInfo, JsonBuildDefinition buildDef, 
        string teamProjectName, string tpcUrl, string defaultBranch, string rootFolder)
    {
        if (xamlDumpInfo.SourceControlMappings == null)
        {
            return;
        }
        else
        {
            var toMapping = new Repository();

            toMapping.Type = "TfsVersionControl";
            toMapping.Name = teamProjectName;
            toMapping.DefaultBranch = defaultBranch;
            toMapping.RootFolder = rootFolder;
            toMapping.Url = tpcUrl;

            toMapping.Properties = new();

            toMapping.Properties.Add("cleanOptions", "3");
            toMapping.Properties.Add("labelSources", "0");
            toMapping.Properties.Add("labelSourcesFormat", "$(build.buildNumber)");

            
            toMapping.Properties.Add("tfvcMapping", 
                SourceControlMappingsToJson(xamlDumpInfo.SourceControlMappings));

            buildDef.Repository = toMapping;
        }
    }

    public string SourceControlMappingsToJson(
            List<TfvcSourceControlMapping> fromMappings)
    {
        var toMappings = new List<TfvcSourceMapping>();

        TfvcSourceMapping toValue;

        foreach (var fromValue in fromMappings)
        {
            toValue = new TfvcSourceMapping();

            if (fromValue.IsCloaked == false)
            {
                toValue.ServerPath = fromValue.ServerPath;
                toValue.MappingType = "map";
                toValue.LocalPath = fromValue.LocalPath;
            }
            else
            {
                toValue.ServerPath = fromValue.ServerPath;
                toValue.MappingType = "cloak";
                toValue.LocalPath = string.Empty;

            }
            toMappings.Add(toValue);
        }

        var returnValue = System.Text.Json.JsonSerializer.Serialize(
            new TfvcSourceMappings() { Mappings = toMappings.ToArray() },
            new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });

        return returnValue;
    }

    private void SetVariables(XamlBuildDumpInfo xamlDumpInfo, JsonBuildDefinition buildDef)
    {
        if (xamlDumpInfo.Parameters == null)
        {
            return;
        }
        else
        {
            foreach (var fromParameterKey in xamlDumpInfo.Parameters.Settings.Keys)
            {
                if (buildDef.Variables.ContainsKey(fromParameterKey) == false)
                {
                    var toValue = new VariableValue();

                    toValue.Value = xamlDumpInfo.Parameters.Settings[fromParameterKey];

                    buildDef.Variables.Add(fromParameterKey, toValue);
                }
                else
                {
                    buildDef.Variables[fromParameterKey].Value = xamlDumpInfo.Parameters.Settings[fromParameterKey];
                }
            }
        }
    }
    protected override async Task OnExecute()
    {
        // XamlBuildRunInfo
        _TeamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        _BuildDefinitionName = Arguments.GetStringValue(Constants.ArgumentNameBuildDefinitionName);

        var originalValue = Arguments.GetStringValue(Constants.ArgumentNameFilename);

        var xamlDumpFilename = string.Empty;
        var upgradeFromXaml = false;

        XamlBuildDumpInfo? xamlDumpInfo = null;

        if (Arguments.HasValue(Constants.ArgumentNameXamlDumpFilename) == true)
        {
            upgradeFromXaml = true;

            xamlDumpFilename = Arguments.GetStringValue(Constants.ArgumentNameXamlDumpFilename);

            xamlDumpFilename = CommandFrameworkUtilities.GetPathToSourceFile(xamlDumpFilename, true);

            xamlDumpInfo =
                System.Text.Json.JsonSerializer.Deserialize<XamlBuildDumpInfo>(
                    File.ReadAllText(xamlDumpFilename)) ??
                        throw new KnownException($"Problem reading json file '{xamlDumpFilename}'");
        }

        _Filename =
            CommandFrameworkUtilities.GetPathToSourceFile(originalValue, true);

        string json = File.ReadAllText(_Filename);

        // read lines from string
        string[] lines = json.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

        JsonBuildDefinition buildDef =
            System.Text.Json.JsonSerializer.Deserialize<JsonBuildDefinition>(json) ??
                throw new KnownException($"Problem reading json file '{_Filename}'");

        if (upgradeFromXaml == true && xamlDumpInfo != null)
        {
            SetXamlInfoToJsonBuild(
                xamlDumpInfo, buildDef, _TeamProjectName, 
                Configuration.CollectionUrl, "$/", $"$/{_TeamProjectName}");
        }

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