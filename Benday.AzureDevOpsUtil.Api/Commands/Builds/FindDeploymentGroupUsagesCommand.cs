using System.Text;
using System.Text.Json;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Commands.ProjectAdministration;
using Benday.AzureDevOpsUtil.Api.DeploymentGroups;
using Benday.AzureDevOpsUtil.Api.Messages.Releases;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.Builds;

[Command(
    Category = Constants.Category_Builds,
    Name = Constants.CommandName_FindDeploymentGroupUsages,
    Description =
        "Read the deployment groups and deployment group agents for one or every team " +
        "project, then trace which release definitions deploy to each group -- including " +
        "which target machines each phase's tag filter actually selects. Deployment " +
        "groups only exist in classic release pipelines, so builds have nothing to scan.",
    IsAsync = true)]
public class FindDeploymentGroupUsagesCommand : AzureDevOpsCommandBase
{
    public DeploymentGroupUsageReport? LastResult { get; private set; }

    public FindDeploymentGroupUsagesCommand(
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
            .WithDescription("Output one CSV row per release phase to deployment group usage");

        arguments.AddBoolean(Constants.CommandArgumentNameToJson)
            .AllowEmptyValue()
            .WithDefaultValue(false)
            .AsNotRequired()
            .WithDescription("Output results as JSON");

        return arguments;
    }

    protected override async Task OnExecute()
    {
        if (Arguments.HasValue(Constants.ArgumentNameAllProjects) == false &&
            Arguments.HasValue(Constants.ArgumentNameTeamProjectName) == false)
        {
            throw new KnownException(
                $"You must specify either /{Constants.ArgumentNameAllProjects} or supply a value for /{Constants.ArgumentNameTeamProjectName}.");
        }
        else if (Arguments.GetBooleanValue(Constants.ArgumentNameAllProjects) == true &&
            Arguments.HasValue(Constants.ArgumentNameTeamProjectName) == true)
        {
            throw new KnownException(
                $"You cannot specify both /{Constants.ArgumentNameAllProjects} and /{Constants.ArgumentNameTeamProjectName} at the same time.");
        }

        var toJson = Arguments.GetBooleanValue(Constants.CommandArgumentNameToJson);
        var toCsv = Arguments.GetBooleanValue(Constants.ArgumentNameOutputCsv);

        var projectNames = await GetProjectNames();

        var report = new DeploymentGroupUsageReport();

        foreach (var projectName in projectNames)
        {
            if (toJson == false && toCsv == false && IsQuietMode == false)
            {
                WriteLine($"Scanning '{projectName}'...");
            }

            report.Projects.Add(await ScanProject(projectName));
        }

        LastResult = report;

        if (IsQuietMode == true)
        {
            return;
        }

        if (toJson == true)
        {
            WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                WriteIndented = true
            }));

            return;
        }

        if (toCsv == true)
        {
            WriteCsv(report);

            return;
        }

        WriteReport(report);
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

    private async Task<DeploymentGroupUsageProject> ScanProject(string projectName)
    {
        var projectEscaped = HttpUtility.UrlPathEncode(projectName);

        var groups = await GetDeploymentGroups(projectEscaped);

        var targetsByGroupId = new Dictionary<int, List<DeploymentTargetInfo>>();

        foreach (var group in groups)
        {
            targetsByGroupId[group.Id] = await GetTargets(projectEscaped, group.Id);
        }

        var phases = await GetDeploymentGroupPhases(projectEscaped);

        return DeploymentGroupUsageAnalyzer.Analyze(
            projectName, groups, targetsByGroupId, phases);
    }

    private async Task<List<DeploymentGroupInfo>> GetDeploymentGroups(string projectEscaped)
    {
        // The deployment groups API only exists in preview form in the 5.x wave,
        // which is what Azure DevOps Server 2019 speaks; newer servers accept it too.
        var requestUrl =
            $"{projectEscaped}/_apis/distributedtask/deploymentgroups?api-version=5.0-preview.1";

        var response = await CallEndpointViaGetAndGetResult<DeploymentGroupListResponse>(
            requestUrl, throwExceptionOnError: false);

        return response?.Value ?? new List<DeploymentGroupInfo>();
    }

    private async Task<List<DeploymentTargetInfo>> GetTargets(string projectEscaped, int groupId)
    {
        var requestUrl =
            $"{projectEscaped}/_apis/distributedtask/deploymentgroups/{groupId}/targets?api-version=5.0-preview.1";

        var response = await CallEndpointViaGetAndGetResult<DeploymentTargetListResponse>(
            requestUrl, throwExceptionOnError: false);

        return response?.Value ?? new List<DeploymentTargetInfo>();
    }

    private async Task<List<DeploymentGroupPhaseReference>> GetDeploymentGroupPhases(
        string projectEscaped)
    {
        var results = new List<DeploymentGroupPhaseReference>();

        var listUrl = $"{projectEscaped}/_apis/release/definitions?api-version=5.0";

        GetReleasesForProjectResponse? list;

        try
        {
            list = await CallEndpointViaGetAndGetResult<GetReleasesForProjectResponse>(
                listUrl, throwExceptionOnError: false,
                azureDevOpsUrlTargetType: AzureDevOpsUrlTargetType.Release);
        }
        catch
        {
            return results;
        }

        if (list?.Releases == null)
        {
            return results;
        }

        foreach (var release in list.Releases)
        {
            string? json;

            try
            {
                json = await GetReleaseDefinitionJson(projectEscaped, release.Id);
            }
            catch
            {
                continue;
            }

            results.AddRange(ReleaseDefinitionDeploymentGroupScanner.FindPhases(json));
        }

        return results;
    }

    private async Task<string?> GetReleaseDefinitionJson(string projectEscaped, int releaseId)
    {
        var requestUrl = $"{projectEscaped}/_apis/release/definitions/{releaseId}?api-version=5.0";

        using var client = GetHttpClientInstanceForAzureDevOps(AzureDevOpsUrlTargetType.Release);

        var result = await client.GetAsync(requestUrl);

        if (result.IsSuccessStatusCode == false)
        {
            return null;
        }

        return await result.Content.ReadAsStringAsync();
    }

    private void WriteCsv(DeploymentGroupUsageReport report)
    {
        var builder = new StringBuilder();

        builder.AppendLine(
            "Project,DeploymentGroup,DeploymentGroupId,ReleaseDefinition,Environment," +
            "Phase,PhaseTags,MatchingTargets");

        foreach (var project in report.Projects)
        {
            foreach (var group in project.Groups)
            {
                foreach (var consumer in group.Consumers)
                {
                    builder.AppendLine(string.Join(',',
                        CsvEscape(project.ProjectName),
                        CsvEscape(group.Name),
                        group.Id.ToString(),
                        CsvEscape(consumer.ReleaseDefinitionName),
                        CsvEscape(consumer.EnvironmentName),
                        CsvEscape(consumer.PhaseName),
                        CsvEscape(string.Join("; ", consumer.Tags)),
                        CsvEscape(string.Join("; ", consumer.MatchingTargetNames))));
                }
            }
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

    private void WriteReport(DeploymentGroupUsageReport report)
    {
        foreach (var project in report.Projects)
        {
            if (project.Groups.Count == 0 && project.PhasesWithUnknownGroup.Count == 0)
            {
                continue;
            }

            WriteLine();
            WriteLine($"** Team Project: {project.ProjectName}");

            foreach (var group in project.Groups)
            {
                WriteLine();
                WriteLine($"   Deployment Group: {group.Name} (id: {group.Id}, targets: {group.Targets.Count})");

                if (string.IsNullOrWhiteSpace(group.Description) == false)
                {
                    WriteLine($"      Description: {group.Description}");
                }

                foreach (var target in group.Targets
                    .OrderBy(x => x.Agent?.Name, StringComparer.OrdinalIgnoreCase))
                {
                    var agentName = target.Agent?.Name ?? $"(target {target.Id})";
                    var status = target.Agent?.Status ?? "unknown";
                    var enabledSuffix = target.Agent?.Enabled == false ? ", disabled" : string.Empty;
                    var tags = target.Tags.Count == 0
                        ? "(none)" : string.Join(", ", target.Tags);

                    WriteLine($"      - {agentName} [{status}{enabledSuffix}] tags: {tags}");
                }

                if (group.Consumers.Count == 0)
                {
                    WriteLine("      Not referenced by any release definition in this project.");
                }
                else
                {
                    WriteLine("      Used by:");

                    foreach (var consumer in group.Consumers
                        .OrderBy(x => x.ReleaseDefinitionName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(x => x.EnvironmentName, StringComparer.OrdinalIgnoreCase))
                    {
                        var tagFilter = consumer.Tags.Count == 0
                            ? "(no tag filter -- every target)"
                            : $"tags: {string.Join(", ", consumer.Tags)}";

                        var matches = consumer.MatchingTargetNames.Count == 0
                            ? "matches NO targets"
                            : $"matches: {string.Join(", ", consumer.MatchingTargetNames)}";

                        WriteLine(
                            $"      - '{consumer.ReleaseDefinitionName}' / " +
                            $"'{consumer.EnvironmentName}' / '{consumer.PhaseName}' " +
                            $"{tagFilter} -> {matches}");
                    }
                }
            }

            if (project.PhasesWithUnknownGroup.Count > 0)
            {
                WriteLine();
                WriteLine("   Phases that reference a deployment group id that no longer exists:");

                foreach (var phase in project.PhasesWithUnknownGroup)
                {
                    WriteLine(
                        $"      - '{phase.ReleaseDefinitionName}' / '{phase.EnvironmentName}' / " +
                        $"'{phase.PhaseName}' -> group id {phase.DeploymentGroupId}");
                }
            }
        }
    }
}
