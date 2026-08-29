using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.AzureDevOpsUtil.Api.TaskGroups;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.Builds;

[Command(
    Category = Constants.Category_Builds,
    Name = Constants.CommandName_InlineTaskGroup,
    Description = "Inline a task group's steps into a build definition and disable the original task group reference.")]
public class InlineTaskGroupCommand : AzureDevOpsCommandBase
{
    public InlineResult? LastResult { get; private set; }

    public InlineTaskGroupCommand(
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

        arguments.AddString(Constants.ArgumentNameTaskGroupId)
            .AsNotRequired()
            .WithDescription("Optional. Inline only this task group id. Default inlines all task groups in the definition.");

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
        var taskGroupIdFilter = Arguments.HasValue(Constants.ArgumentNameTaskGroupId)
            ? Arguments.GetStringValue(Constants.ArgumentNameTaskGroupId)
            : null;
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

        var references = BuildDefinitionTaskGroupScanner.FindReferences(rawJson);

        if (taskGroupIdFilter != null)
        {
            references = references
                .Where(r => string.Equals(r.TaskGroupId, taskGroupIdFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (references.Count == 0)
        {
            WriteLine(taskGroupIdFilter == null
                ? $"No task group references found in build definition '{buildDefName}'."
                : $"Build definition '{buildDefName}' does not reference task group id '{taskGroupIdFilter}'.");
            LastResult = new InlineResult();
            return;
        }

        var taskGroupJsonsById = await FetchReferencedTaskGroups(teamProjectName, references);

        var node = JsonNode.Parse(rawJson) ??
            throw new InvalidOperationException("Failed to parse build definition JSON.");

        var inliner = new BuildDefinitionInliner(taskGroupJsonsById);
        var inlineResult = inliner.Inline(node, taskGroupIdFilter);

        LastResult = inlineResult;

        var modifiedJson = node.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

        if (dryRun)
        {
            WriteDryRunOutput(outputDir, buildDefName, rawJson, modifiedJson, inlineResult);
        }
        else
        {
            await PutBuildDefinition(teamProjectName, buildDef.Id, node.ToJsonString());
            WriteLine();
            WriteLine($"Inlined {inlineResult.InlinedReferenceCount} task group reference(s) " +
                      $"({inlineResult.InlinedTaskGroupIds.Count} unique task group(s)) " +
                      $"into build definition '{buildDefName}' (id: {buildDef.Id}).");
        }
    }

    private async Task<BuildDefinitionInfo?> FindBuildDefinition(string teamProjectName, string buildDefName)
    {
        var projectEscaped = HttpUtility.UrlPathEncode(teamProjectName);
        var nameEscaped = HttpUtility.UrlEncode(buildDefName);
        var requestUrl =
            $"{projectEscaped}/_apis/build/definitions?api-version=7.1&name={nameEscaped}";

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
            $"{projectEscaped}/_apis/build/definitions/{buildDefinitionId}?api-version=7.1";

        return await GetStringAsync(requestUrl);
    }

    private async Task<Dictionary<string, JsonNode>> FetchReferencedTaskGroups(
        string teamProjectName, IEnumerable<TaskGroupReference> references)
    {
        var uniqueIds = references
            .Select(r => r.TaskGroupId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        using var http = GetHttpClientInstanceForAzureDevOps();
        var client = new TaskGroupClient(http);

        var result = new Dictionary<string, JsonNode>(StringComparer.OrdinalIgnoreCase);

        foreach (var id in uniqueIds)
        {
            var rawJson = await client.GetRawJsonByIdAsync(teamProjectName, id);
            var listNode = JsonNode.Parse(rawJson) ??
                throw new InvalidOperationException(
                    $"Failed to parse task group response for id '{id}'.");

            var first = (listNode["value"] as JsonArray)?.FirstOrDefault();
            if (first == null)
            {
                throw new KnownException(
                    $"Task group '{id}' was not found in team project '{teamProjectName}'.");
            }

            result[id] = first;
        }

        return result;
    }

    private async Task PutBuildDefinition(string teamProjectName, int buildDefinitionId, string bodyJson)
    {
        var projectEscaped = HttpUtility.UrlPathEncode(teamProjectName);
        var requestUrl =
            $"{projectEscaped}/_apis/build/definitions/{buildDefinitionId}?api-version=7.1";

        await SendPutForBodySingleAttempt(requestUrl, bodyJson);
    }

    private void WriteDryRunOutput(
        string outputDir, string buildDefName, string beforeJson, string afterJson, InlineResult inlineResult)
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
        WriteLine($"  Inlined {inlineResult.InlinedReferenceCount} task group reference(s) " +
                  $"({inlineResult.InlinedTaskGroupIds.Count} unique task group(s)).");
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
