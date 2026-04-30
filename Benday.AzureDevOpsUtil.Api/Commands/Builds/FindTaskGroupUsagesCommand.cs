using System.Text.Json;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.AzureDevOpsUtil.Api.Messages.TaskGroups;
using Benday.AzureDevOpsUtil.Api.TaskGroups;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.Builds;

[Command(
    Category = Constants.Category_Builds,
    Name = Constants.CommandName_FindTaskGroupUsages,
    Description = "Find build definitions that reference task groups in a team project.",
    IsAsync = true)]
public class FindTaskGroupUsagesCommand : AzureDevOpsCommandBase
{
    public List<TaskGroupUsage>? LastResult { get; private set; }

    public FindTaskGroupUsagesCommand(
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

        arguments.AddString(Constants.ArgumentNameTaskGroupId)
            .AsNotRequired()
            .WithDescription("Optional. Filter to only references of this task group id.");

        arguments.AddBoolean(Constants.CommandArgumentNameToJson)
            .AllowEmptyValue()
            .AsNotRequired()
            .WithDescription("Output results as JSON");

        return arguments;
    }

    protected override async Task OnExecute()
    {
        var teamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        var taskGroupIdFilter = Arguments.HasValue(Constants.ArgumentNameTaskGroupId)
            ? Arguments.GetStringValue(Constants.ArgumentNameTaskGroupId)
            : null;
        var toJson = Arguments.GetBooleanValue(Constants.CommandArgumentNameToJson);

        var taskGroupNamesById = await GetTaskGroupNameLookup(teamProjectName);
        var buildDefinitions = await GetBuildDefinitions(teamProjectName);

        if (IsQuietMode == false && toJson == false)
        {
            WriteLine($"Scanning {buildDefinitions.Count} build definition(s) in '{teamProjectName}'...");
        }

        var usages = new List<TaskGroupUsage>();

        foreach (var def in buildDefinitions)
        {
            var json = await GetBuildDefinitionJson(teamProjectName, def.Id);
            if (string.IsNullOrEmpty(json))
            {
                continue;
            }

            var refs = BuildDefinitionTaskGroupScanner.FindReferences(json);

            foreach (var reference in refs)
            {
                if (taskGroupIdFilter != null &&
                    !string.Equals(reference.TaskGroupId, taskGroupIdFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                taskGroupNamesById.TryGetValue(reference.TaskGroupId, out var name);

                usages.Add(new TaskGroupUsage
                {
                    BuildDefinitionId = def.Id,
                    BuildDefinitionName = def.Name,
                    TaskGroupId = reference.TaskGroupId,
                    TaskGroupName = name ?? "(unknown)",
                    VersionSpec = reference.VersionSpec,
                    PhaseIndex = reference.PhaseIndex,
                    PhaseName = reference.PhaseName,
                    StepIndex = reference.StepIndex,
                    StepDisplayName = reference.StepDisplayName,
                    Enabled = reference.Enabled
                });
            }
        }

        LastResult = usages;

        if (IsQuietMode)
        {
            return;
        }

        if (toJson)
        {
            WriteLine(JsonSerializer.Serialize(usages, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
            return;
        }

        WriteHumanReadable(usages, taskGroupIdFilter);
    }

    private void WriteHumanReadable(List<TaskGroupUsage> usages, string? taskGroupIdFilter)
    {
        WriteLine();
        WriteLine($"Total task group references found: {usages.Count}");

        if (usages.Count == 0)
        {
            if (taskGroupIdFilter != null)
            {
                WriteLine($"No build definitions reference task group '{taskGroupIdFilter}'.");
            }
            return;
        }

        var grouped = usages
            .GroupBy(x => x.TaskGroupId)
            .OrderBy(g => usages.First(u => u.TaskGroupId == g.Key).TaskGroupName, StringComparer.OrdinalIgnoreCase);

        foreach (var group in grouped)
        {
            var first = group.First();
            WriteLine();
            WriteLine($"** Task Group: {first.TaskGroupName} (id: {first.TaskGroupId})");
            WriteLine($"   Used by {group.Count()} step(s) across {group.Select(x => x.BuildDefinitionId).Distinct().Count()} build definition(s):");

            foreach (var usage in group.OrderBy(x => x.BuildDefinitionName, StringComparer.OrdinalIgnoreCase))
            {
                var enabledMarker = usage.Enabled ? "enabled " : "disabled";
                WriteLine($"   - [{enabledMarker}] {usage.BuildDefinitionName} (build def id: {usage.BuildDefinitionId})");
                WriteLine($"       phase[{usage.PhaseIndex}] '{usage.PhaseName}' / step[{usage.StepIndex}] '{usage.StepDisplayName}' (versionSpec: {usage.VersionSpec})");
            }
        }
    }

    private async Task<Dictionary<string, string>> GetTaskGroupNameLookup(string teamProjectName)
    {
        using var http = GetHttpClientInstanceForAzureDevOps();
        var client = new TaskGroupClient(http);
        var taskGroups = await client.ListAsync(teamProjectName);

        return taskGroups
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<List<BuildDefinitionInfo>> GetBuildDefinitions(string teamProjectName)
    {
        var projectEscaped = HttpUtility.UrlPathEncode(teamProjectName);
        var requestUrl = $"{projectEscaped}/_apis/build/definitions?api-version=7.1";

        var response = await CallEndpointViaGetAndGetResult<BuildDefinitionInfoResponse>(requestUrl);

        return response?.Values ?? new List<BuildDefinitionInfo>();
    }

    private async Task<string?> GetBuildDefinitionJson(string teamProjectName, int buildDefinitionId)
    {
        var projectEscaped = HttpUtility.UrlPathEncode(teamProjectName);
        var requestUrl =
            $"{projectEscaped}/_apis/build/definitions/{buildDefinitionId}?api-version=7.1";

        return await GetStringAsync(requestUrl);
    }
}
