using System.Text;
using System.Text.Json;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Commands.ProjectAdministration;
using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.AzureDevOpsUtil.Api.NuGetTasks;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.Builds;

[Command(
    Category = Constants.Category_Builds,
    Name = Constants.CommandName_FindNuGetToolInstaller,
    Description =
        "Find the classic build definitions that use the NuGet tool installer task " +
        "(NuGetToolInstaller) and report which version of the task each step uses " +
        "and which version of NuGet it installs.")]
public class FindNuGetToolInstallerCommand : AzureDevOpsCommandBase
{
    public List<NuGetToolInstallerUsage>? LastResult { get; private set; }

    public FindNuGetToolInstallerCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);

        arguments.AddString(Constants.ArgumentNameTeamProjectName)
            .AsNotRequired()
            .WithDescription("Team project name");

        arguments.AddBoolean(Constants.ArgumentNameAllProjects)
            .AllowEmptyValue()
            .WithDefaultValue(false)
            .AsNotRequired()
            .WithDescription("Scan every project in this collection");

        arguments.AddBoolean(Constants.ArgumentNameOutputCsv)
            .AllowEmptyValue()
            .WithDefaultValue(false)
            .AsNotRequired()
            .WithDescription("Output results in CSV format");

        arguments.AddBoolean(Constants.CommandArgumentNameToJson)
            .AllowEmptyValue()
            .WithDefaultValue(false)
            .AsNotRequired()
            .WithDescription("Output results as JSON");

        return arguments;
    }

    protected override async Task OnExecute(CancellationToken cancellationToken)
    {
        if (Arguments.HasValue(Constants.ArgumentNameAllProjects) == false &&
            Arguments.HasValue(Constants.ArgumentNameTeamProjectName) == false)
        {
            throw new KnownException(
                $"You must specify either --{Constants.ArgumentNameAllProjects} or supply a value for --{Constants.ArgumentNameTeamProjectName}.");
        }
        else if (Arguments.GetBooleanValue(Constants.ArgumentNameAllProjects) == true &&
            Arguments.HasValue(Constants.ArgumentNameTeamProjectName) == true)
        {
            throw new KnownException(
                $"You cannot specify both --{Constants.ArgumentNameAllProjects} and --{Constants.ArgumentNameTeamProjectName} at the same time.");
        }

        var toJson = Arguments.GetBooleanValue(Constants.CommandArgumentNameToJson);
        var toCsv = Arguments.GetBooleanValue(Constants.ArgumentNameOutputCsv);

        var projectNames = await GetProjectNames();

        var results = new List<NuGetToolInstallerUsage>();

        foreach (var projectName in projectNames)
        {
            if (toJson == false && toCsv == false && IsQuietMode == false)
            {
                WriteLine($"Scanning '{projectName}'...");
            }

            results.AddRange(await ScanBuilds(projectName));
        }

        LastResult = results;

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

        if (toCsv == true)
        {
            WriteCsv(results);

            return;
        }

        WriteReport(results);
    }

    private async Task<List<string>> GetProjectNames()
    {
        if (Arguments.GetBooleanValue(Constants.ArgumentNameAllProjects) == true)
        {
            var command = new ListTeamProjectsCommand(
                ExecutionInfo.GetCloneOfArguments(Constants.CommandName_ListProjects, true),
                _OutputProvider);

            await command.ExecuteAsync();

            if (command.LastResult == null || command.LastResult.Projects.Length == 0)
            {
                throw new KnownException("No team projects found.");
            }

            return command.LastResult.Projects
                .Select(x => x.Name)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return new List<string> { Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName) };
    }

    private async Task<List<NuGetToolInstallerUsage>> ScanBuilds(string projectName)
    {
        var results = new List<NuGetToolInstallerUsage>();

        var projectEscaped = HttpUtility.UrlPathEncode(projectName);

        // api-version 5.0 is the newest that Azure DevOps Server 2019 accepts, and
        // newer servers accept it too -- these commands exist for a TFS 2019 upgrade.
        var listUrl = $"{projectEscaped}/_apis/build/definitions?api-version=5.0";

        var list = await CallEndpointViaGetAndGetResult<BuildDefinitionInfoResponse>(listUrl);

        if (list?.Values == null)
        {
            return results;
        }

        foreach (var definition in list.Values)
        {
            string? json;

            try
            {
                var detailUrl = $"{projectEscaped}/_apis/build/definitions/{definition.Id}?api-version=5.0";
                json = await GetStringAsync(detailUrl, false, false);
            }
            catch
            {
                continue;
            }

            foreach (var reference in NuGetToolInstallerScanner.FindReferences(json))
            {
                results.Add(new NuGetToolInstallerUsage
                {
                    ProjectName = projectName,
                    BuildDefinitionId = definition.Id,
                    BuildDefinitionName = definition.Name,
                    PhaseIndex = reference.PhaseIndex,
                    PhaseName = reference.PhaseName,
                    StepIndex = reference.StepIndex,
                    StepDisplayName = reference.StepDisplayName,
                    Enabled = reference.Enabled,
                    TaskVersionSpec = reference.TaskVersionSpec,
                    NuGetVersionSpec = reference.NuGetVersionSpec,
                    CheckLatest = reference.CheckLatest
                });
            }
        }

        return results;
    }

    private void WriteCsv(List<NuGetToolInstallerUsage> results)
    {
        var builder = new StringBuilder();

        builder.AppendLine(
            "Project,BuildDefinition,BuildDefinitionId,Phase,StepIndex," +
            "StepDisplayName,Enabled,TaskVersion,NuGetVersion,CheckLatest");

        foreach (var usage in results)
        {
            builder.AppendLine(string.Join(',',
                CsvEscape(usage.ProjectName),
                CsvEscape(usage.BuildDefinitionName),
                usage.BuildDefinitionId.ToString(),
                CsvEscape(usage.PhaseName),
                usage.StepIndex.ToString(),
                CsvEscape(usage.StepDisplayName),
                usage.Enabled.ToString(),
                CsvEscape(usage.TaskVersionSpec),
                CsvEscape(usage.NuGetVersionSpec),
                CsvEscape(usage.CheckLatest)));
        }

        WriteLine(builder.ToString().TrimEnd());
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') == true || value.Contains('"') == true)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }

    private void WriteReport(List<NuGetToolInstallerUsage> results)
    {
        WriteLine();
        WriteLine($"NuGet tool installer steps found: {results.Count}");

        if (results.Count == 0)
        {
            return;
        }

        foreach (var projectGroup in results
            .GroupBy(x => x.ProjectName)
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            WriteLine();
            WriteLine($"** Team Project: {projectGroup.Key}");

            foreach (var buildGroup in projectGroup
                .GroupBy(x => x.BuildDefinitionName)
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                var first = buildGroup.First();

                WriteLine($"   {buildGroup.Key} (id: {first.BuildDefinitionId})");

                foreach (var usage in buildGroup
                    .OrderBy(x => x.PhaseIndex)
                    .ThenBy(x => x.StepIndex))
                {
                    var disabledSuffix = usage.Enabled == true ? string.Empty : " [DISABLED]";
                    var nugetVersion = string.IsNullOrEmpty(usage.NuGetVersionSpec)
                        ? "(not set)" : usage.NuGetVersionSpec;
                    var checkLatestSuffix = string.IsNullOrEmpty(usage.CheckLatest)
                        ? string.Empty : $", check latest: {usage.CheckLatest}";

                    WriteLine(
                        $"       - '{usage.StepDisplayName}'{disabledSuffix} -- " +
                        $"task version: {usage.TaskVersionSpec}, " +
                        $"nuget version: {nugetVersion}{checkLatestSuffix}");
                }
            }
        }
    }
}
