using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.AzureDevOpsUtil.Api.NuGetTasks;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.Builds;

[Command(
    Category = Constants.Category_Builds,
    Name = Constants.CommandName_UpdateNuGetToolInstaller,
    Description =
        "Update the NuGet tool installer steps (NuGetToolInstaller) in a classic build " +
        "definition to a chosen task version and NuGet version, set each step's display " +
        "name to show the NuGet version, and save the change back to the server.")]
public class UpdateNuGetToolInstallerCommand : AzureDevOpsCommandBase
{
    public NuGetToolInstallerUpdateResult? LastResult { get; private set; }

    public UpdateNuGetToolInstallerCommand(
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

        arguments.AddString(Constants.ArgumentNameBuildDefinitionName)
            .AsRequired()
            .WithDescription("Build definition name");

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
            .WithDescription("Write before/after JSON files locally instead of updating the build definition on the server.");

        arguments.AddString(Constants.ArgumentNameExportToPath)
            .AsNotRequired()
            .WithDescription("Directory for dry-run output files. Default is the current working directory.");

        return arguments;
    }

    protected override async Task OnExecute(CancellationToken cancellationToken)
    {
        var teamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        var buildDefName = Arguments.GetStringValue(Constants.ArgumentNameBuildDefinitionName);
        var nugetVersion = Arguments.GetStringValue(Constants.ArgumentNameNuGetVersion);
        var taskVersion = Arguments.GetStringValue(Constants.ArgumentNameTaskVersion);
        var dryRun = Arguments.GetBooleanValue(Constants.ArgumentNameDryRun);
        var outputDir = Arguments.HasValue(Constants.ArgumentNameExportToPath)
            ? Arguments.GetStringValue(Constants.ArgumentNameExportToPath)
            : Directory.GetCurrentDirectory();

        var buildDef = await FindBuildDefinition(teamProjectName, buildDefName);
        if (buildDef == null)
        {
            throw new KnownException(
                $"Build definition '{buildDefName}' was not found in team project '{teamProjectName}'.");
        }

        var rawJson = await GetBuildDefinitionFullJson(teamProjectName, buildDef.Id);
        if (string.IsNullOrEmpty(rawJson))
        {
            throw new InvalidOperationException(
                $"Failed to retrieve full build definition JSON for id {buildDef.Id}.");
        }

        var node = JsonNode.Parse(rawJson) ??
            throw new InvalidOperationException("Failed to parse build definition JSON.");

        var updater = new NuGetToolInstallerUpdater(taskVersion, nugetVersion);
        var updateResult = updater.Update(node);

        LastResult = updateResult;

        if (updateResult.UpdatedStepCount == 0)
        {
            WriteLine(
                $"Build definition '{buildDefName}' has no NuGet tool installer steps. " +
                "Nothing to update.");
            return;
        }

        if (dryRun == true)
        {
            WriteDryRunOutput(outputDir, buildDefName, rawJson,
                node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
                updateResult);
        }
        else
        {
            await PutBuildDefinition(teamProjectName, buildDef.Id, node.ToJsonString());

            WriteLine();
            WriteLine(
                $"Updated {updateResult.UpdatedStepCount} NuGet tool installer step(s) in build " +
                $"definition '{buildDefName}' (id: {buildDef.Id}) to task version '{taskVersion}' " +
                $"and NuGet version '{nugetVersion}'.");

            WriteChanges(updateResult);
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

    private async Task<BuildDefinitionInfo?> FindBuildDefinition(string teamProjectName, string buildDefName)
    {
        var projectEscaped = HttpUtility.UrlPathEncode(teamProjectName);
        var nameEscaped = HttpUtility.UrlEncode(buildDefName);

        // api-version 5.0 is the newest that Azure DevOps Server 2019 accepts, and
        // newer servers accept it too -- these commands exist for a TFS 2019 upgrade.
        var requestUrl =
            $"{projectEscaped}/_apis/build/definitions?api-version=5.0&name={nameEscaped}";

        var response = await CallEndpointViaGetAndGetResult<BuildDefinitionInfoResponse>(requestUrl);

        if (response == null || response.Values.Count == 0)
        {
            return null;
        }

        return response.Values.FirstOrDefault(d =>
            string.Equals(d.Name, buildDefName, StringComparison.OrdinalIgnoreCase))
            ?? response.Values[0];
    }

    private async Task<string?> GetBuildDefinitionFullJson(string teamProjectName, int buildDefinitionId)
    {
        var projectEscaped = HttpUtility.UrlPathEncode(teamProjectName);
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
        string outputDir, string buildDefName, string beforeJson, string afterJson,
        NuGetToolInstallerUpdateResult updateResult)
    {
        Directory.CreateDirectory(outputDir);

        var safeName = MakeFileSafe(buildDefName);
        var beforePath = Path.Combine(outputDir, $"{safeName}.before.json");
        var afterPath = Path.Combine(outputDir, $"{safeName}.after.json");

        var prettyBefore = TryPrettyPrint(beforeJson) ?? beforeJson;

        File.WriteAllText(beforePath, prettyBefore);
        File.WriteAllText(afterPath, afterJson);

        WriteLine();
        WriteLine($"Dry run: did not push changes to the server.");
        WriteLine($"  Would update {updateResult.UpdatedStepCount} NuGet tool installer step(s).");

        WriteChanges(updateResult);

        WriteLine($"  Before: {beforePath}");
        WriteLine($"  After:  {afterPath}");
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
