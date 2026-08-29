using System.Text.Json;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Commands.ProjectAdministration;
using Benday.AzureDevOpsUtil.Api.Demands;
using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.AzureDevOpsUtil.Api.Messages.Releases;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.Builds;

[Command(
    Category = Constants.Category_Builds,
    Name = Constants.CommandName_FindDemands,
    Description =
        "Find the build and release definitions that have agent demands, and list " +
        "the demands each one carries. Demands are the capabilities a definition " +
        "requires of an agent, so this is the companion to the agent capability " +
        "commands. Scans both builds and releases unless /builds or /releases is " +
        "given.")]
public class FindDemandsCommand : AzureDevOpsCommandBase
{
    public List<DefinitionDemands>? LastResult { get; private set; }

    public FindDemandsCommand(
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

        arguments.AddBoolean(Constants.ArgumentNameBuildsScope)
            .AllowEmptyValue()
            .WithDefaultValue(false)
            .AsNotRequired()
            .WithDescription("Only scan build definitions");

        arguments.AddBoolean(Constants.ArgumentNameReleasesScope)
            .AllowEmptyValue()
            .WithDefaultValue(false)
            .AsNotRequired()
            .WithDescription("Only scan release definitions");

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

        // Neither flag means scan both; either flag narrows to just that kind.
        var onlyBuilds = Arguments.GetBooleanValue(Constants.ArgumentNameBuildsScope);
        var onlyReleases = Arguments.GetBooleanValue(Constants.ArgumentNameReleasesScope);
        var scanBuilds = onlyBuilds == true || onlyReleases == false;
        var scanReleases = onlyReleases == true || onlyBuilds == false;

        var projectNames = await GetProjectNames();

        var results = new List<DefinitionDemands>();

        foreach (var projectName in projectNames)
        {
            if (toJson == false && IsQuietMode == false)
            {
                WriteLine($"Scanning '{projectName}'...");
            }

            if (scanBuilds == true)
            {
                results.AddRange(await ScanBuilds(projectName));
            }

            if (scanReleases == true)
            {
                results.AddRange(await ScanReleases(projectName));
            }
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

    private async Task<List<DefinitionDemands>> ScanBuilds(string projectName)
    {
        var results = new List<DefinitionDemands>();

        var projectEscaped = HttpUtility.UrlPathEncode(projectName);
        var listUrl = $"{projectEscaped}/_apis/build/definitions?api-version=7.1";

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
                var detailUrl = $"{projectEscaped}/_apis/build/definitions/{definition.Id}?api-version=7.1";
                json = await GetStringAsync(detailUrl, false, false);
            }
            catch
            {
                continue;
            }

            var demands = DemandScanner.Scan(json);

            if (demands.Count == 0)
            {
                continue;
            }

            results.Add(new DefinitionDemands
            {
                DefinitionType = "Build",
                ProjectName = projectName,
                Id = definition.Id,
                Name = definition.Name,
                PoolOrQueue = definition.Queue?.Pool?.Name ?? string.Empty,
                Demands = demands.ToList()
            });
        }

        return results;
    }

    private async Task<List<DefinitionDemands>> ScanReleases(string projectName)
    {
        var results = new List<DefinitionDemands>();

        var projectEscaped = HttpUtility.UrlPathEncode(projectName);
        var listUrl = $"{projectEscaped}/_apis/release/definitions?api-version=7.1";

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

            var demands = DemandScanner.Scan(json);

            if (demands.Count == 0)
            {
                continue;
            }

            results.Add(new DefinitionDemands
            {
                DefinitionType = "Release",
                ProjectName = projectName,
                Id = release.Id,
                Name = release.Name,
                PoolOrQueue = string.Empty,
                Demands = demands.ToList()
            });
        }

        return results;
    }

    private async Task<string?> GetReleaseDefinitionJson(string projectEscaped, int releaseId)
    {
        var requestUrl = $"{projectEscaped}/_apis/release/definitions/{releaseId}?api-version=7.1";

        using var client = GetHttpClientInstanceForAzureDevOps(AzureDevOpsUrlTargetType.Release);

        var result = await client.GetAsync(requestUrl);

        if (result.IsSuccessStatusCode == false)
        {
            return null;
        }

        return await result.Content.ReadAsStringAsync();
    }

    private void WriteReport(List<DefinitionDemands> results)
    {
        WriteLine();
        WriteLine($"Definitions with demands: {results.Count}");

        if (results.Count == 0)
        {
            return;
        }

        foreach (var group in results
            .GroupBy(x => x.ProjectName)
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            WriteLine();
            WriteLine($"** Team Project: {group.Key}");

            foreach (var definition in group
                .OrderBy(x => x.DefinitionType, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                var queueSuffix = string.IsNullOrEmpty(definition.PoolOrQueue)
                    ? string.Empty
                    : $", pool: {definition.PoolOrQueue}";

                WriteLine($"   [{definition.DefinitionType}] {definition.Name} (id: {definition.Id}{queueSuffix})");

                foreach (var demand in definition.Demands)
                {
                    WriteLine($"       - {demand}");
                }
            }
        }
    }
}
