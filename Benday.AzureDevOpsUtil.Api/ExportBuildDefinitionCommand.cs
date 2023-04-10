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

        arguments.AddBoolean(Constants.ArgumentNameShowLastRunInfo)
            .AllowEmptyValue()
            .WithDescription("Show last build run info")
            .AsNotRequired();
        
        arguments.AddBoolean(Constants.ArgumentNameOutputRaw)
            .AllowEmptyValue()
            .AsNotRequired()
            .WithDescription("Output raw build definition")
            .AsNotRequired();

        return arguments;
    }

    protected override async Task OnExecute()
    {
        // XamlBuildRunInfo
        _TeamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        _BuildDefinitionName = Arguments.GetStringValue(Constants.ArgumentNameBuildDefinitionName);
        var isXamlMode = Arguments.GetBooleanValue(Constants.ArgumentNameXaml);
        var outputRaw = Arguments.GetBooleanValue(Constants.ArgumentNameOutputRaw);
        var showLastRunInfo = Arguments.GetBooleanValue(Constants.ArgumentNameShowLastRunInfo);

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
            else if (outputRaw == true)
            {
                WriteLine(json);
            }
            else
            {
                var data = JsonUtilities.GetJsonValueAsType<XamlBuildDefinitionDetail>(json);

                var builder = new StringBuilder();

                builder.AppendLabeledValue("Id", data.Id);
                builder.AppendLabeledValue("Name", data.Name);
                builder.AppendLabeledValue("BuildType", data.BuildType);
                builder.AppendLabeledValue("DefaultDropLocation", data.DefaultDropLocation);
                builder.AppendLabeledValue("BuildArgs", data.BuildArgs);
                builder.AppendLabeledValue("CreatedOn", data.CreatedOn);
                builder.AppendLabeledValue("LastBuild Id", data.LastBuild.Id);
                builder.AppendLabeledValue("LastBuild Url", data.LastBuild.Url);
                builder.AppendLabeledValue("Repository Type", data.Repository.RepositoryType);

                if (string.IsNullOrWhiteSpace(data.Repository.Properties.TfvcMapping) == false)
                {
                    try
                    {
                        var mappings = JsonUtilities.GetJsonValueAsType<XamlBuildTfvcMappings>(
                            data.Repository.Properties.TfvcMapping);

                        if (mappings == null)
                        {
                            builder.AppendLabeledValue("Repository Properties", "(n/a)");
                        }
                        else
                        {
                            builder.AppendLabeledValue("Repository Properties", string.Empty);

                            var count = 0;

                            foreach (var mapping in mappings.Mappings)
                            {
                                count++;

                                builder.AppendLabeledValue($"\tMapping #{count}", string.Empty);
                                builder.AppendLabeledValue("\tMapping Type", mapping.MappingType);
                                builder.AppendLabeledValue("\tServer Path", mapping.ServerPath);
                                builder.AppendLabeledValue("\tLocal Path", mapping.LocalPath ?? string.Empty);
                                builder.AppendLine();
                            }
                        }
                    }
                    catch 
                    {
                        builder.AppendLabeledValue("Repository Properties",
                            data.Repository.Properties.TfvcMapping);
                    }                    
                }
                else
                {
                    builder.AppendLabeledValue("Repository Properties", "(n/a)");
                }
                
                builder.AppendLabeledValue("Project Name", data.Project.Name);
                builder.AppendLabeledValue("Project Id", data.Project.Id);
                builder.AppendLabeledValue("Controller Id", data.Controller.Id);
                builder.AppendLabeledValue("Controller Name", data.Controller.Name);

                if (showLastRunInfo == true)
                {
                    await AppendLastRunInfo(builder, data);
                }


                WriteLine(builder.ToString());
            }
        }
    }

    private async Task AppendLastRunInfo(StringBuilder builder, XamlBuildDefinitionDetail definition)
    {
        string requestUrl;

        if (Arguments.GetBooleanValue(Constants.ArgumentNameXaml) == true)
        {
            requestUrl = $"{definition.LastBuild.Url}?api-version=2.2";
        }
        else
        {
            requestUrl = $"{definition.LastBuild.Url}";
        }

        var result = await CallEndpointViaGetAndGetResult<XamlBuildRunInfo>(requestUrl);

        if (result == null)
        {
            return;
        }
        else
        {
            builder.AppendLabeledValue("Build Number", result.BuildNumber);
            builder.AppendLabeledValue("Build Reason", result.BuildReason);
            builder.AppendLabeledValue("Queued At", result.QueueTime);
            builder.AppendLabeledValue("Started At", result.StartTime);
            builder.AppendLabeledValue("Finished At", result.FinishTime);
            builder.AppendLabeledValue("Last Changed Date", result.LastChangedDate);
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
