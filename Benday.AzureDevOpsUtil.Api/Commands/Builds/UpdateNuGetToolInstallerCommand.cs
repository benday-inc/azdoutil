using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;

using Benday.AzureDevOpsUtil.Api.NuGetTasks;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.Builds;

[Command(
    Category = Constants.Category_Builds,
    Name = Constants.CommandName_UpdateNuGetToolInstaller,
    Description =
        "Update the NuGet tool installer steps (NuGetToolInstaller) in classic build " +
        "definitions to a chosen task version and NuGet version, set each step's display " +
        "name to show the NuGet version, and save the change back to the server. Build " +
        "definitions that already match are left alone.")]
public class UpdateNuGetToolInstallerCommand : AzureDevOpsCommandBase
{
    /// <summary>
    /// Result for the last build definition that was updated. When more than one build
    /// definition is updated, use <see cref="LastResults"/> instead.
    /// </summary>
    public NuGetToolInstallerUpdateResult? LastResult { get; private set; }

    /// <summary>
    /// One entry per build definition that was actually patched.
    /// </summary>
    public List<BuildDefinitionNuGetToolInstallerUpdate> LastResults { get; private set; } = new();

    /// <summary>
    /// Build definitions that use the task but already match the requested versions, so
    /// they were not written to the server.
    /// </summary>
    public int AlreadyCurrentDefinitionCount { get; private set; }

    public UpdateNuGetToolInstallerCommand(
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
            .WithDescription("Update build definitions in every project in this collection");

        arguments.AddString(Constants.ArgumentNameBuildDefinitionName)
            .AsNotRequired()
            .WithDescription(
                "Build definition name. If omitted, every build definition in scope that " +
                "uses the NuGet tool installer task is considered.");

        arguments.AddString(Constants.ArgumentNameNuGetVersion)
            .AsNotRequired()
            .WithDefaultValue(Constants.DefaultNuGetVersionSpec)
            .WithDescription(
                $"Version of NuGet the task should install. Default is '{Constants.DefaultNuGetVersionSpec}'.");

        arguments.AddString(Constants.ArgumentNameTaskVersion)
            .AsNotRequired()
            .WithDefaultValue(Constants.DefaultNuGetToolInstallerTaskVersion)
            .WithDescription(
                $"Version spec for the NuGetToolInstaller task itself. Default is '{Constants.DefaultNuGetToolInstallerTaskVersion}'.");

        arguments.AddBoolean(Constants.ArgumentNameDryRun)
            .AllowEmptyValue()
            .AsNotRequired()
            .WithDescription("Write before/after JSON files locally instead of updating the build definitions on the server.");

        arguments.AddString(Constants.ArgumentNameExportToPath)
            .AsNotRequired()
            .WithDescription("Directory for dry-run output files. Default is the current working directory.");

        return arguments;
    }

    protected override async Task OnExecute(CancellationToken cancellationToken)
    {
        var allProjects = Arguments.GetBooleanValue(Constants.ArgumentNameAllProjects);

        if (allProjects == false &&
            Arguments.HasValue(Constants.ArgumentNameTeamProjectName) == false)
        {
            throw new KnownException(
                $"You must specify either --{Constants.ArgumentNameAllProjects} or supply a value for --{Constants.ArgumentNameTeamProjectName}.");
        }
        else if (allProjects == true &&
            Arguments.HasValue(Constants.ArgumentNameTeamProjectName) == true)
        {
            throw new KnownException(
                $"You cannot specify both --{Constants.ArgumentNameAllProjects} and --{Constants.ArgumentNameTeamProjectName} at the same time.");
        }

        var buildDefName = Arguments.HasValue(Constants.ArgumentNameBuildDefinitionName) == true
            ? Arguments.GetStringValue(Constants.ArgumentNameBuildDefinitionName)
            : string.Empty;

        var nugetVersion = Arguments.GetStringValue(Constants.ArgumentNameNuGetVersion);
        var taskVersion = Arguments.GetStringValue(Constants.ArgumentNameTaskVersion);
        var dryRun = Arguments.GetBooleanValue(Constants.ArgumentNameDryRun);
        var outputDir = Arguments.HasValue(Constants.ArgumentNameExportToPath)
            ? Arguments.GetStringValue(Constants.ArgumentNameExportToPath)
            : Directory.GetCurrentDirectory();

        // one updater for the whole run, so the expected display name is decided in the
        // same place that writes it
        var updater = new NuGetToolInstallerUpdater(taskVersion, nugetVersion);

        if (IsQuietMode == false)
        {
            WriteLine("Looking for build definitions that use the NuGet tool installer task...");
        }

        var usages = await FindUsages();

        if (string.IsNullOrEmpty(buildDefName) == false)
        {
            usages = usages
                .Where(x => string.Equals(
                    x.BuildDefinitionName, buildDefName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (usages.Count == 0)
            {
                throw new KnownException(
                    $"Build definition '{buildDefName}' was not found, or it has no NuGet " +
                    "tool installer steps.");
            }
        }

        var definitionCount = CountDefinitions(usages);

        // only the definitions that are actually out of spec get written; a definition
        // whose steps already match is left alone rather than taking a no-op revision
        var definitionsToPatch = usages
            .Where(updater.IsOutOfSpec)
            .GroupBy(x => (x.ProjectName, x.BuildDefinitionId))
            .OrderBy(x => x.Key.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.First().BuildDefinitionName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        AlreadyCurrentDefinitionCount = definitionCount - definitionsToPatch.Count;

        var results = new List<BuildDefinitionNuGetToolInstallerUpdate>();

        foreach (var group in definitionsToPatch)
        {
            var update = await UpdateDefinition(
                group.Key.ProjectName, group.Key.BuildDefinitionId,
                group.First().BuildDefinitionName,
                updater, taskVersion, nugetVersion, dryRun, outputDir, allProjects);

            if (update != null)
            {
                results.Add(update);
            }
        }

        LastResults = results;
        LastResult = results.LastOrDefault()?.Result;

        if (IsQuietMode == true)
        {
            return;
        }

        WriteSummary(results, definitionCount, taskVersion, nugetVersion, dryRun);
    }

    /// <summary>
    /// Runs the find command rather than repeating its scan. It runs quiet, so its own
    /// report stays out of the way of this command's reporting.
    /// </summary>
    private async Task<List<NuGetToolInstallerUsage>> FindUsages()
    {
        var command = await ExecuteAzdoCommandAsync<FindNuGetToolInstallerCommand>(args =>
        {
            args.Set(Constants.ArgumentNameAllProjects,
                Arguments.GetBooleanValue(Constants.ArgumentNameAllProjects));

            CopyArgumentIfSupplied(args, Constants.ArgumentNameTeamProjectName);
        });

        return command.LastResult ?? new List<NuGetToolInstallerUsage>();
    }

    private static int CountDefinitions(List<NuGetToolInstallerUsage> usages)
    {
        return usages
            .Select(x => (x.ProjectName, x.BuildDefinitionId))
            .Distinct()
            .Count();
    }

    private async Task<BuildDefinitionNuGetToolInstallerUpdate?> UpdateDefinition(
        string teamProjectName, int buildDefinitionId, string buildDefinitionName,
        NuGetToolInstallerUpdater updater, string taskVersion, string nugetVersion,
        bool dryRun, string outputDir, bool allProjects)
    {
        var rawJson = await GetBuildDefinitionFullJson(teamProjectName, buildDefinitionId);

        if (string.IsNullOrEmpty(rawJson))
        {
            throw new InvalidOperationException(
                $"Failed to retrieve full build definition JSON for id {buildDefinitionId}.");
        }

        var node = JsonNode.Parse(rawJson) ??
            throw new InvalidOperationException("Failed to parse build definition JSON.");

        var updateResult = updater.Update(node);

        if (updateResult.UpdatedStepCount == 0)
        {
            // the find run said this definition used the task, so it changed underneath us
            return null;
        }

        var update = new BuildDefinitionNuGetToolInstallerUpdate
        {
            ProjectName = teamProjectName,
            BuildDefinitionId = buildDefinitionId,
            BuildDefinitionName = buildDefinitionName,
            Result = updateResult
        };

        if (dryRun == true)
        {
            WriteDryRunOutput(outputDir, update, rawJson,
                node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
                allProjects);
        }
        else
        {
            await PutBuildDefinition(teamProjectName, buildDefinitionId, node.ToJsonString());
        }

        if (IsQuietMode == false)
        {
            WriteLine();

            var prefix = dryRun == true ? "Would update" : "Updated";

            WriteLine(
                $"{prefix} {updateResult.UpdatedStepCount} NuGet tool installer step(s) in build " +
                $"definition '{buildDefinitionName}' (project: {teamProjectName}, " +
                $"id: {buildDefinitionId}) to task version '{taskVersion}' and NuGet " +
                $"version '{nugetVersion}'.");

            WriteChanges(updateResult);

            if (dryRun == true)
            {
                WriteLine($"  Before: {update.BeforeFilePath}");
                WriteLine($"  After:  {update.AfterFilePath}");
            }
        }

        return update;
    }

    private void WriteSummary(
        List<BuildDefinitionNuGetToolInstallerUpdate> results, int definitionCount,
        string taskVersion, string nugetVersion, bool dryRun)
    {
        WriteLine();

        if (definitionCount == 0)
        {
            WriteLine("No build definitions use the NuGet tool installer task. Nothing to update.");

            return;
        }

        if (results.Count == 0)
        {
            WriteLine(
                $"All {definitionCount} build definition(s) that use the NuGet tool installer " +
                $"task already use task version '{taskVersion}' and NuGet version " +
                $"'{nugetVersion}'. Nothing to update.");

            return;
        }

        var stepCount = results.Sum(x => x.Result.UpdatedStepCount);
        var verb = dryRun == true ? "Dry run: would have updated" : "Updated";

        WriteLine(
            $"{verb} {stepCount} NuGet tool installer step(s) in {results.Count} of " +
            $"{definitionCount} build definition(s) to task version '{taskVersion}' and " +
            $"NuGet version '{nugetVersion}'.");

        if (AlreadyCurrentDefinitionCount > 0)
        {
            WriteLine(
                $"Left {AlreadyCurrentDefinitionCount} build definition(s) alone because they " +
                "already matched.");
        }

        if (dryRun == true)
        {
            WriteLine("No changes were pushed to the server.");
        }
    }

    private void WriteChanges(NuGetToolInstallerUpdateResult updateResult)
    {
        foreach (var change in updateResult.Changes)
        {
            WriteLine(
                $"   [{change.PhaseName}] step {change.StepIndex}: " +
                $"task {change.OldTaskVersionSpec} -> {change.NewTaskVersionSpec}, " +
                $"nuget '{change.OldNuGetVersionSpec}' -> '{change.NewNuGetVersionSpec}', " +
                $"display name '{change.OldDisplayName}' -> '{change.NewDisplayName}'");
        }
    }

    private async Task<string?> GetBuildDefinitionFullJson(string teamProjectName, int buildDefinitionId)
    {
        var projectEscaped = HttpUtility.UrlPathEncode(teamProjectName);

        // api-version 5.0 is the newest that Azure DevOps Server 2019 accepts, and
        // newer servers accept it too -- these commands exist for a TFS 2019 upgrade.
        var requestUrl =
            $"{projectEscaped}/_apis/build/definitions/{buildDefinitionId}?api-version=5.0";

        return await GetStringAsync(requestUrl);
    }

    private async Task PutBuildDefinition(string teamProjectName, int buildDefinitionId, string bodyJson)
    {
        var projectEscaped = HttpUtility.UrlPathEncode(teamProjectName);
        var requestUrl =
            $"{projectEscaped}/_apis/build/definitions/{buildDefinitionId}?api-version=5.0";

        await SendPutForBodySingleAttempt(requestUrl, bodyJson);
    }

    private void WriteDryRunOutput(
        string outputDir, BuildDefinitionNuGetToolInstallerUpdate update,
        string beforeJson, string afterJson, bool allProjects)
    {
        Directory.CreateDirectory(outputDir);

        // when more than one project is in scope, build definition names are only unique
        // within a project, so the project name goes in the file name too
        var safeName = allProjects == true
            ? $"{MakeFileSafe(update.ProjectName)}.{MakeFileSafe(update.BuildDefinitionName)}"
            : MakeFileSafe(update.BuildDefinitionName);

        update.BeforeFilePath = Path.Combine(outputDir, $"{safeName}.before.json");
        update.AfterFilePath = Path.Combine(outputDir, $"{safeName}.after.json");

        var prettyBefore = TryPrettyPrint(beforeJson) ?? beforeJson;

        File.WriteAllText(update.BeforeFilePath, prettyBefore);
        File.WriteAllText(update.AfterFilePath, afterJson);
    }

    private static string? TryPrettyPrint(string json)
    {
        try
        {
            var node = JsonNode.Parse(json);
            return node?.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return null;
        }
    }

    private static string MakeFileSafe(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }
}
