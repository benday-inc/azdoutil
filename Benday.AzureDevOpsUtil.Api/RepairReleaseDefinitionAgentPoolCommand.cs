/*
using System.Net;
using System.Text;
using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.AzureDevOpsUtil.Api.Messages.AgentPools;
using Benday.AzureDevOpsUtil.Api.Messages.BuildQueues;
using Benday.AzureDevOpsUtil.Api.Messages.ReleaseDefinitionDetails;
using Benday.AzureDevOpsUtil.Api.Messages.Releases;
using Benday.CommandsFramework;
using Benday.JsonUtilities;

namespace Benday.AzureDevOpsUtil.Api;

[Command(
    Category = Constants.Category_Builds,
    Name = Constants.CommandArgumentNameRepairReleaseDefAgentPool,
        Description = "Repairs the agent pool setting for the release definitions in a team project or team projects. This is helpful after an on-prem to cloud migration.",
        IsAsync = true)]
public class RepairReleaseDefinitionAgentPoolCommand : AzureDevOpsCommandBase
{
    private string _TeamProjectName = string.Empty;
    private const string PRINT_JSON_ON_PREVIEW = "PrintJsonOnPreview";

    public GetReleasesForProjectResponse? LastResult { get; private set; }

    public RepairReleaseDefinitionAgentPoolCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);

        arguments.AddString(Constants.ArgumentNameTeamProjectName)
            .WithDescription("Team project name").
            AsNotRequired();

        arguments.AddBoolean(Constants.ArgumentNameAllProjects)
            .AllowEmptyValue()
            .WithDescription("All releases in all projects in this collection")
            .AsNotRequired();

        arguments.AddBoolean(PRINT_JSON_ON_PREVIEW).AllowEmptyValue().WithDefaultValue(false).AsNotRequired().WithDescription("Print modified json in preview mode");

        arguments.AddBoolean(Constants.ArgumentNamePreviewOnly).WithDescription("Preview only. Do not update release definitions.").AsNotRequired().AllowEmptyValue().WithDefaultValue(false);

        arguments.AddFile(Constants.ArgumentNameOriginalQueueInfo)
            .WithDescription("Queue info JSON file from on-prem server. Assumes that pools have been recreated in the cloud using the same name.")
            .MustExist()
            .AsRequired().FromPositionalArgument(2);

        arguments.AddFile(Constants.ArgumentNameOriginalReleaseInfo)
            .WithDescription("Release def JSON file from on-prem server. Assumes that pools have been recreated in the cloud using the same name.")
            .MustExist()
            .AsRequired().FromPositionalArgument(1);



        return arguments;
    }

    private async Task<GetAgentPoolsResponse?> GetAgentPools()
    {
        var command = new ListAgentPoolsCommand(
            ExecutionInfo.GetCloneOfArguments(
               Constants.CommandArgumentNameListAgentPools, true), _OutputProvider);

        await command.ExecuteAsync();

        return command.LastResult;
    }

    private async Task<GetBuildQueuesResponse?> GetBuildQueues(string teamProjectName)
    {
        var requestUrl = $"{teamProjectName}/_apis/distributedtask/queues?api-version=7.1";

        var result = await CallEndpointViaGetAndGetResult<GetBuildQueuesResponse>(requestUrl);

        return result;
    }

    protected override async Task OnExecute()
    {
        var previewOnly = Arguments.GetBooleanValue(Constants.ArgumentNamePreviewOnly);

        if (Arguments.HasValue(Constants.ArgumentNameAllProjects) == false &&
            Arguments.HasValue(Constants.ArgumentNameTeamProjectName) == false)
        {
            throw new KnownException(
                $"You must specify either /{Constants.ArgumentNameAllProjects} or supply a value for /{Constants.ArgumentNameTeamProjectName}.");
        }
        else if (Arguments.HasValue(Constants.ArgumentNameAllProjects) == true &&
            Arguments.HasValue(Constants.ArgumentNameTeamProjectName) == true)
        {
            throw new KnownException(
                $"You cannot specify both /{Constants.ArgumentNameAllProjects} and /{Constants.ArgumentNameTeamProjectName} at the same time.");
        }

        bool allProjects;

        if (Arguments.GetBooleanValue(Constants.ArgumentNameAllProjects) == true)
        {
            _TeamProjectName = string.Empty;
            allProjects = true;
        }
        else
        {
            _TeamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
            allProjects = false;
        }

        var origQueueInfoFilePath = Arguments.GetPathToFile(Constants.ArgumentNameOriginalQueueInfo);

        var originalQueueInfos = 
            JsonSerializer.Deserialize<List<BuildQueueInfo>>(
                File.ReadAllText(origQueueInfoFilePath));

        if (originalQueueInfos == null)
        {
            throw new KnownException("Could not deserialize queue info file.");
        }

        var origReleaseInfoFilePath = Arguments.GetPathToFile(Constants.ArgumentNameOriginalReleaseInfo);

        var originalReleaseDefInfos = JsonSerializer.Deserialize<List<GetReleaseDefinitionDetailResponse>>(
            File.ReadAllText(origReleaseInfoFilePath));

        if (originalReleaseDefInfos == null)
        {
            throw new KnownException("Could not deserialize release def info file.");
        }

        if (allProjects == false)
        {
            await RepairForSingleProject(originalReleaseDefInfos, originalQueueInfos, agentPoolInfoCurrent, _TeamProjectName, previewOnly);
        }
        else
        {
            await RepairForAllProjects(originalReleaseDefInfos, originalQueueInfos, previewOnly);
        }

        if (_notUpdated.Count > 0)
        {
            WriteLine("The following build definitions were not updated:");
            foreach (var item in _notUpdated)
            {
                WriteLine(item);
            }
        }
    }

    private async Task RepairForAllProjects(
        List<ReleaseInfo> originalReleaseDefs,
        List<BuildQueueInfo> originalQueueInfos,
        GetAgentPoolsResponse agentPoolInfoCurrent,
        bool previewOnly)
    {
        // call ListTeamProjectsCommand
        var command = new ListTeamProjectsCommand(
            ExecutionInfo.GetCloneOfArguments(
               Constants.CommandName_ListProjects, true), _OutputProvider);

        await command.ExecuteAsync();

        if (command.LastResult == null)
        {
            throw new KnownException("No team projects found.");
        }
        else
        {
            var teamProjects = command.LastResult.Projects;

            var results = new List<BuildDefinitionInfo>();

            foreach (var teamProject in teamProjects)
            {
                await RepairForSingleProject(originalReleaseDefs, originalQueueInfos, agentPoolInfoCurrent, teamProject.Name, previewOnly);
            }
        }
    }

    private async Task RepairForSingleProject(
        List<ReleaseInfo> originalReleaseDefs,
        List<BuildQueueInfo> originalQueueInfos,
        GetAgentPoolsResponse agentPoolInfoCurrent,
        string teamProjectName, bool previewOnly)
    {
        WriteLine($"Getting release definitions for project '{teamProjectName}'...");

        var results = await GetReleasesForProject(teamProjectName);

        if (results == null)
        {
            WriteLine(String.Empty);
            WriteLine($"No build definitions found for project '{teamProjectName}'");
        }
        else
        {
            var currentQueues = await GetBuildQueues(teamProjectName);

            if (currentQueues == null)
            {
                throw new KnownException("Could not get current build queues from Azure DevOps.");
            }

            WriteLine(String.Empty);

            WriteLine($"Result count for project '{teamProjectName}': {results.Count}");

            foreach (var releaseDefInfo in results.Releases)
            {
                await RepairAgentPoolForBuildDef(
                    originalReleaseDefs, originalQueueInfos, 
                    currentQueues, agentPoolInfoCurrent, releaseDefInfo, previewOnly);
            }
        }
    }

    private async Task UpdateBuildDefinition(BuildDefinitionInfo buildDefInfo, string updatedBuildDef)
    {
        var requestUrl = $"{buildDefInfo.Project.Name}/_apis/build/definitions/{buildDefInfo.Id}?api-version=7.1";

        WriteLine($"Sending update request for build definition '{buildDefInfo.Name}' in '{buildDefInfo.Project.Name}'...");

        try
        {
            await SendPutForBodySingleAttempt(requestUrl, updatedBuildDef, true);

            WriteLine($"Updated build definition '{buildDefInfo.Name}' in '{buildDefInfo.Project.Name}' with new agent pool id.");
            WriteLine();
        }
        catch (Exception ex)
        {
            var message = $"Error updating build definition '{buildDefInfo.Name}' in '{buildDefInfo.Project.Name}': {ex.Message}";

            _notUpdated.Add(message);
        }
    }

    private async Task RepairAgentPoolForBuildDef(
        List<GetReleaseDefinitionDetailResponse> originalReleaseDefs,
        GetBuildQueuesResponse currentQueues,
        GetAgentPoolsResponse agentPoolInfoCurrent,
        ReleaseInfo releaseDefInfo, bool previewOnly)
    {
        var execInfo = ExecutionInfo.GetCloneOfArguments(
             Constants.CommandArgumentNameExportReleaseDefinition,
             true);

        execInfo.Arguments.Add(
            Constants.ArgumentNameReleaseDefinitionName,
            releaseDefInfo.Name);

        if (execInfo.Arguments.ContainsKey(Constants.ArgumentNameAllProjects) == true)
        {
            execInfo.Arguments.Remove(Constants.ArgumentNameAllProjects);
        }

        if (execInfo.Arguments.ContainsKey(Constants.ArgumentNameTeamProjectName) == false)
        {
            execInfo.Arguments.Add(
                Constants.ArgumentNameTeamProjectName,
                releaseDefInfo.ProjectReference.Name);
        }

        var command = new ExportReleaseDefinitionCommand(
         execInfo, _OutputProvider);

        await command.ExecuteAsync();

        var releaseDefJson = command.LastResultRawJson;

        if (String.IsNullOrWhiteSpace(releaseDefJson) == true)
        {
            throw new InvalidOperationException(
                $"Could not get build definition JSON for build '{releaseDefInfo.Name}' in project '{releaseDefInfo.Project.Name}'.");
        }

        var releaseDef = command.LastResult;

        if (releaseDef == null)
        {
            throw new InvalidOperationException("Could not get build definition from last result.");
        }

        var originalReleaseDef = originalReleaseDefs.
                Where(x => 
                    x.Name == releaseDef.Name && 
                    x.ProjectReference.Name == releaseDefInfo.ProjectReference.Name).FirstOrDefault();

        if (originalReleaseDef == null)
        {
            var message = $"Could not find original release definition with name '{releaseDef.Name}' in project '{releaseDefInfo.ProjectReference.Name}'.";

            _notUpdated.Add(message);

            WriteLine(message + ": skipping");

            return;
        }

        originalReleaseDef.Queue.

        var originalPoolName = originalReleaseDef.Queue.Pool?.Name;

        if (string.IsNullOrEmpty(originalPoolName) == true)
        {
            var message = $"Could not find original pool with name '{originalPoolName}' for build '{releaseDefInfo.Name}' in project '{releaseDefInfo.Project.Name}'";

            _notUpdated.Add(message);

            WriteLine(message + ": skipping");

            return;
        }

        var originalQueueName = originalReleaseDef.Queue.Name;

        if (string.IsNullOrEmpty(originalQueueName) == true)
        {
            var message = $"Could not find original with name '{originalQueueName}' for build '{releaseDefInfo.Name}' in project '{releaseDefInfo.Project.Name}'";

            _notUpdated.Add(message);

            WriteLine(message + ": skipping");

            return;
        }

        var originalPoolId = originalReleaseDef.Queue.Pool?.Id ?? -1;

        var currentPool = agentPoolInfoCurrent.Pools
            .Where(x => x.Name.Equals(originalPoolName, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

        if (currentPool == null)
        {
            var message = $"Could not find current pool with name '{originalPoolName}' for build '{releaseDefInfo.Name}' in project '{releaseDefInfo.Project.Name}'";

            // throw new InvalidOperationException(message);

            _notUpdated.Add(message);

            WriteLine(message + ": skipping");

            return;
        }

        var currentQueue = currentQueues.Value
           .Where(x => x.Name.Equals(originalQueueName, StringComparison.OrdinalIgnoreCase))
           .FirstOrDefault();

        if (currentQueue == null)
        {
            throw new InvalidOperationException($"Could not find current queue with name '{originalQueueName}' for build '{releaseDefInfo.Name}' in project '{releaseDefInfo.Project.Name}'");
        }

        string updatedBuildDef = UpdateQueueAndVerifyJobAuthorizationScope(releaseDefJson, currentQueue, releaseDefInfo.Project.Name);

        if (previewOnly == true)
        {
            WriteLine("PREVIEW ONLY: not updating build definition in azure devops");

            if (Arguments.GetBooleanValue(PRINT_JSON_ON_PREVIEW) == true)
            {
                WriteLine(updatedBuildDef);
            }
        }
        else
        {
            await UpdateBuildDefinition(releaseDefInfo, updatedBuildDef);
        }
    }

    private List<string> _notUpdated = new List<string>();

    private string UpdateQueueAndVerifyJobAuthorizationScope(
        string buildDefJson, BuildQueueInfo currentQueue, string teamProjectName)
    {
        var editor = new JsonEditor(buildDefJson, true);

        var name = editor.GetValue("name");

        var jobAuthorizationScope = editor.GetValue("jobAuthorizationScope");

        WriteLine($"Updating build def '{name}' in '{teamProjectName}' queue and pool...");

        editor.SetValue(currentQueue.Pool.Id, "queue", "pool", "id");
        editor.SetValue(currentQueue.Pool.Name, "queue", "pool", "name");
        editor.SetValue(currentQueue.Pool.IsHosted, "queue", "pool", "isHosted");

        editor.SetValue(currentQueue.Id, "queue", "id");
        editor.SetValue(currentQueue.Name, "queue", "name");

        var json = editor.ToJson(true);

        var invalidJobAuthorizationScope = "\"jobAuthorizationScope\": 0";
        var validJobAuthorizationScope = $"\"jobAuthorizationScope\": \"{jobAuthorizationScope}\"";

        if (json.Contains(invalidJobAuthorizationScope) == true)
        {
            WriteLine($"Updating build def '{name}' in '{teamProjectName}' job authorization scope to '{jobAuthorizationScope}'...");
            json = json.Replace(invalidJobAuthorizationScope, validJobAuthorizationScope);
        }

        return json;
    }

    private async Task<GetReleasesForProjectResponse?> GetReleasesForProject(string teamProjectName)
    {
        var requestUrl = $"{teamProjectName}/_apis/release/releases?api-version=7.1";

        var result = await CallEndpointViaGetAndGetResult<GetReleasesForProjectResponse>(
            requestUrl, azureDevOpsUrlTargetType: AzureDevOpsUrlTargetType.Release);

        LastResult = result;

        return result;
    }

    private string ToString(BuildDefinitionInfo definition)
    {
        var builder = new StringBuilder();
        builder.Append(definition.Name);

        if (Arguments.GetBooleanValue(Constants.ArgumentNameNameOnly) == false)
        {
            builder.Append(" (");
            builder.Append(definition.Id);
            builder.Append(")");

            if (definition.Queue != null && definition.Queue.Pool != null)
            {
                builder.Append(" - [Queue Name: ");
                builder.Append(definition.Queue.Pool.Name);
                builder.Append(", Queue Pool Id: ");
                builder.Append(definition.Queue.Pool.Id);
                builder.Append(", Pool Is Hosted: ");
                builder.Append(definition.Queue.Pool.IsHosted);
                builder.Append("]");
            }
        }

        return builder.ToString();
    }
}
*/