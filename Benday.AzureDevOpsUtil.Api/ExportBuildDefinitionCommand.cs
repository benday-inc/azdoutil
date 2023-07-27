using System.Net;
using System.Text;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(
    Category = Constants.Category_Builds,
    Name = Constants.CommandArgumentNameExportBuildDefinition,
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

        arguments.AddBoolean(Constants.ArgumentNameOutputCsv)
            .AllowEmptyValue()
            .WithDescription("Output results in CSV format")
            .AsNotRequired();

        arguments.AddBoolean(Constants.ArgumentNameNoCsvHeader)
            .AllowEmptyValue()
            .WithDescription("Do not print the CSV column header info")
            .AsNotRequired();

        arguments.AddBoolean(Constants.ArgumentNameOutputRaw)
            .AllowEmptyValue()
            .AsNotRequired()
            .WithDescription("Output raw build definition")
            .AsNotRequired();

        return arguments;
    }

    private bool _isXamlMode;

    protected override async Task OnExecute()
    {
        // XamlBuildRunInfo
        _TeamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        _BuildDefinitionName = Arguments.GetStringValue(Constants.ArgumentNameBuildDefinitionName);
        _isXamlMode = Arguments.GetBooleanValue(Constants.ArgumentNameXaml);
        var outputRaw = Arguments.GetBooleanValue(Constants.ArgumentNameOutputRaw);
        var showLastRunInfo = Arguments.GetBooleanValue(Constants.ArgumentNameShowLastRunInfo);
        var outputCsv = Arguments.GetBooleanValue(Constants.ArgumentNameOutputCsv);
        var noCsvHeader = Arguments.GetBooleanValue(Constants.ArgumentNameNoCsvHeader);

        var buildId = await GetBuildIdByBuildName(_BuildDefinitionName);

        if (buildId == null)
        {
            throw new KnownException(
                String.Format("Build name '{0}' was not found.", _BuildDefinitionName));
        }
        else
        {
            var apiVersion = "7.0";

            if (_isXamlMode == true)
            {
                apiVersion = "2.0";
            }

            var requestUrl = $"{_TeamProjectName}/_apis/build/definitions/{buildId}?api-version={apiVersion}&includeLatestBuilds=true";

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
            else if (outputCsv == true)
            {
                var data = JsonUtilities.GetJsonValueAsType<XamlBuildDefinitionDetail>(json);

                var builder = new StringBuilder();

                await WriteToCsv(showLastRunInfo, data, builder, noCsvHeader);

                using var reader = new StringReader(builder.ToString());

                var line = reader.ReadLine();

                while (line != null)
                {
                    if (string.IsNullOrWhiteSpace(line) == false)
                    {
                        WriteLine(line.Trim());
                    }

                    line = reader.ReadLine();
                }
            }
            else
            {
                var data = JsonUtilities.GetJsonValueAsType<XamlBuildDefinitionDetail>(json);

                var builder = new StringBuilder();

                await WriteToConsoleOutput(showLastRunInfo, data, builder);

                WriteLine(builder.ToString());
            }
        }
    }

    private async Task WriteToConsoleOutput(bool showLastRunInfo, XamlBuildDefinitionDetail data, StringBuilder builder)
    {
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

        if (showLastRunInfo == true && _isXamlMode == true)
        {
            await AppendLastRunInfoForXaml(builder, data);
        }
        else if (showLastRunInfo == true && _isXamlMode == false)
        {
            AppendLastRunInfo(builder, data, false);
        }
    }

    private async Task WriteToCsv(bool showLastRunInfo, XamlBuildDefinitionDetail data, StringBuilder builder,
        bool noCsvHeader)
    {
        if (noCsvHeader == false)
        {
            builder.AppendCsvHeader("Id");
            builder.AppendCsvHeader("Name");
            builder.AppendCsvHeader("BuildType");
            builder.AppendCsvHeader("DefaultDropLocation");
            builder.AppendCsvHeader("BuildArgs");
            builder.AppendCsvHeader("CreatedOn");
            builder.AppendCsvHeader("LastBuild Id");
            builder.AppendCsvHeader("LastBuild Url");
            builder.AppendCsvHeader("Repository Type");
            builder.AppendCsvHeader("Project Name");
            builder.AppendCsvHeader("Project Id");
            builder.AppendCsvHeader("Controller Id");
            builder.AppendCsvHeader("Controller Name");

            if (showLastRunInfo == true)
            {
                builder.AppendCsvHeader("Build Number");
                builder.AppendCsvHeader("Build Reason");
                builder.AppendCsvHeader("Queued At");
                builder.AppendCsvHeader("Started At");
                builder.AppendCsvHeader("Finished At");
                builder.AppendCsvHeader("Last Changed Date");
            }

            builder.AppendLine();
        }

        builder.AppendCsv("Id", data.Id);
        builder.AppendCsv("Name", data.Name);
        builder.AppendCsv("BuildType", data.BuildType);
        builder.AppendCsv("DefaultDropLocation", data.DefaultDropLocation);
        builder.AppendCsv("BuildArgs", data.BuildArgs);
        builder.AppendCsv("CreatedOn", data.CreatedOn);
        builder.AppendCsv("LastBuild Id", data.LastBuild.Id);
        builder.AppendCsv("LastBuild Url", data.LastBuild.Url);
        builder.AppendCsv("Repository Type", data.Repository.RepositoryType);
        builder.AppendCsv("Project Name", data.Project.Name);
        builder.AppendCsv("Project Id", data.Project.Id);
        builder.AppendCsv("Controller Id", data.Controller.Id);
        builder.AppendCsv("Controller Name", data.Controller.Name);

        if (showLastRunInfo == true && _isXamlMode == true)
        {
            await AppendLastRunInfoForXaml(builder, data, true);
        }
        else if (showLastRunInfo == true && _isXamlMode == false)
        {
            AppendLastRunInfo(builder, data, true);
        }

        builder.AppendLine();
    }

    private void AppendLastRunInfo(StringBuilder builder,
        XamlBuildDefinitionDetail definition, bool csv = false)
    {
        if (definition.LatestBuild == null && csv == true)
        {
            builder.AppendCsv("Build Number", string.Empty);
            builder.AppendCsv("Build Reason", string.Empty);
            builder.AppendCsv("Queued At", string.Empty);
            builder.AppendCsv("Started At", string.Empty);
            builder.AppendCsv("Finished At", string.Empty);
            builder.AppendCsv("Last Changed Date", string.Empty);
        }
        else if (definition.LatestBuild == null && csv == false)
        {
            builder.AppendLabeledValue("Latest Build Info", "not available");
        }
        else if (definition.LatestBuild != null && csv == false)
        {
            builder.AppendLabeledValue("Build Number", definition.LatestBuild.BuildNumber);
            builder.AppendLabeledValue("Queued At", definition.LatestBuild.QueueTime);
            builder.AppendLabeledValue("Started At", definition.LatestBuild.StartTime);
            builder.AppendLabeledValue("Finished At", definition.LatestBuild.FinishTime);
            builder.AppendLabeledValue("Result", definition.LatestBuild.Result);
            builder.AppendLabeledValue("Status", definition.LatestBuild.Status);
            builder.AppendLabeledValue("Source Branch", definition.LatestBuild.TriggerInfo.SourceBranch);
            builder.AppendLabeledValue("Source SHA", definition.LatestBuild.TriggerInfo.SourceSha);
            builder.AppendLabeledValue("Source Message", definition.LatestBuild.TriggerInfo.Message);
            builder.AppendLabeledValue("Trigger Repository", definition.LatestBuild.TriggerInfo.TriggerRepository);
        }
        else if (definition.LatestBuild != null && csv == true)
        {
            builder.AppendCsv("Build Number", definition.LatestBuild.BuildNumber);
            builder.AppendCsv("Build Reason", string.Empty);
            builder.AppendCsv("Queued At", definition.LatestBuild.QueueTime);
            builder.AppendCsv("Started At", definition.LatestBuild.StartTime);
            builder.AppendCsv("Finished At", definition.LatestBuild.FinishTime);
            builder.AppendCsv("Last Changed Date", string.Empty);
        }
    }

    private async Task AppendLastRunInfoForXaml(StringBuilder builder,
        XamlBuildDefinitionDetail definition, bool csv = false)
    {
        string requestUrl;

        if (Arguments.GetBooleanValue(Constants.ArgumentNameXaml) == true)
        {
            requestUrl = $"{definition.LastBuild.Url}?api-version=2.2";
        }
        else
        {
            requestUrl = $"{definition.LastBuild.Url}?api-version=7.0";
        }

        var result = await CallEndpointViaGetAndGetResult<XamlBuildRunInfo>(requestUrl);

        if (result == null)
        {
            return;
        }
        else if (csv)
        {
            builder.AppendCsv("Build Number", result.BuildNumber);
            builder.AppendCsv("Build Reason", result.BuildReason);
            builder.AppendCsv("Queued At", result.QueueTime);
            builder.AppendCsv("Started At", result.StartTime);
            builder.AppendCsv("Finished At", result.FinishTime);
            builder.AppendCsv("Last Changed Date", result.LastChangedDate);
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
