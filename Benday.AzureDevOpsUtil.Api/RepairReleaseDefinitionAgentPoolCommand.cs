﻿using System.Net;
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

        arguments.AddFile(Constants.ArgumentNameOriginalReleaseInfo)
            .WithDescription("Release def agent pool references JSON file from on-prem server. Assumes that pools have been recreated in the cloud using the same name.")
            .MustExist()
            .AsRequired().FromPositionalArgument(1);



        return arguments;
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

        var origReleaseInfoFilePath = Arguments.GetPathToFile(Constants.ArgumentNameOriginalReleaseInfo);

        var originalReleaseDefInfos = JsonSerializer.Deserialize<List<ReleaseQueueInfo>>(
            File.ReadAllText(origReleaseInfoFilePath));

        if (originalReleaseDefInfos == null)
        {
            throw new KnownException("Could not deserialize release def agent pool reference info file.");
        }

        if (allProjects == false)
        {
            await RepairForSingleProject(originalReleaseDefInfos, _TeamProjectName, previewOnly);
        }
        else
        {
            await RepairForAllProjects(originalReleaseDefInfos, previewOnly);
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
        List<ReleaseQueueInfo> originalReleaseDefs,
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
                await RepairForSingleProject(originalReleaseDefs, teamProject.Name, previewOnly);
            }
        }
    }

    private async Task RepairForSingleProject(
        List<ReleaseQueueInfo> originalReleaseDefs,
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
                    originalReleaseDefs,  
                    currentQueues, releaseDefInfo, previewOnly, teamProjectName);
            }
        }
    }




    private async Task<GetBuildQueuesResponse?> GetBuildQueues(string teamProjectName)
    {
        var requestUrl = $"{teamProjectName}/_apis/distributedtask/queues?api-version=7.1";

        var result = await CallEndpointViaGetAndGetResult<GetBuildQueuesResponse>(requestUrl);

        return result;
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
        List<ReleaseQueueInfo> originalReleaseDefs,
        GetBuildQueuesResponse currentQueues,
        ReleaseInfo releaseDefInfo, bool previewOnly, string teamProjectName)
    {
        var temp = originalReleaseDefs.Where(x => x.ReleaseName == releaseDefInfo.Name).ToList();

        var projectReleaseDefs = originalReleaseDefs.Where(
            x => x.TeamProjectName ==  teamProjectName)
            .OrderBy(x => x.ReleaseName)
            .ToList();

        var releaseQueueInfo = projectReleaseDefs
            .Where(x => x.ReleaseId == releaseDefInfo.Id &&
                x.ReleaseName == releaseDefInfo.Name)
            .FirstOrDefault();

        if (releaseQueueInfo == null)
        {
            var message = $"Could not find original release definition with id '{releaseDefInfo.Id}' name '{releaseDefInfo.Name}' in project '{teamProjectName}'.";

            _notUpdated.Add(message);

            WriteLine(message + ": skipping");

            return;
        }

        if (releaseQueueInfo.QueueReferences.Count == 0)
        {
            var message = $"No queue references found for release id '{releaseDefInfo.Id}' name '{releaseDefInfo.Name}' in project '{teamProjectName}'.";
            _notUpdated.Add(message);
            WriteLine(message + ": skipping");
            return;
        }

        var releaseDefJson =
            await GetReleaseDefinitionJson(releaseDefInfo, teamProjectName);

        if (String.IsNullOrWhiteSpace(releaseDefJson) == true)
        {
            var message = $"Could not get build definition JSON for release id '{releaseDefInfo.Id}' name '{releaseDefInfo.Name}' in project '{teamProjectName}'.";

            _notUpdated.Add(message);

            WriteLine(message + ": skipping");

            return;
        }


        var editor = new JsonEditor(releaseDefJson, true);

        foreach (var queueRef in releaseQueueInfo.QueueReferences)
        {
            var currentQueue = currentQueues.Value
                .Where(x => x.Name == queueRef.QueueName)
                .FirstOrDefault();

            if (currentQueue == null)
            {
                var message = $"Could not find target queue with name '{queueRef.QueueName}' for release id '{releaseDefInfo.Id}' name '{releaseDefInfo.Name}' in project '{teamProjectName}'.";
                _notUpdated.Add(message);
                WriteLine(message + ": skipping");
                continue;
            }

            UpdateQueueAndVerifyJobAuthorizationScope(editor, currentQueue, queueRef, teamProjectName);
        }

        var updatedBuildDef = editor.ToJson(true);

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
            // await UpdateBuildDefinition(releaseDefInfo, updatedBuildDef);
            throw new NotImplementedException();
        }
    }
    private void UpdateQueueAndVerifyJobAuthorizationScope(
        JsonEditor editor, BuildQueueInfo targetQueue, QueueReference queueRef, string teamProjectName)
    {
        var environmentName = queueRef.EnvironmentName;
        var environmentId = queueRef.EnvironmentId;

        var environment = editor.Root.GetArrayItem(
            "environments", "id", environmentId.ToString());

        var deployPhases = environment.GetArray("deployPhases");

        var deploymentInput = deployPhases.FirstOrDefaultWithPropertyName("deploymentInput");

        if (deploymentInput == null)
        {
            var message = $"Could not find deployment input for environment '{environmentName}' id '{environmentId}' in release definition for project '{teamProjectName}'.";
            _notUpdated.Add(message);

            WriteLine(message + ": skipping");
        }
        else
        {
            var oldQueueId = deploymentInput.GetInt32("queueId");

            deploymentInput["queueId"] = targetQueue.Id;

            WriteLine($"Updated queue id for environment '{environmentName}' id '{environmentId}' from '{oldQueueId}' to '{targetQueue.Id}' in release definition for project '{teamProjectName}'.");
        }
    }

    private async Task<string?> GetReleaseDefinitionJson(ReleaseInfo releaseDefInfo, string teamProjectName)
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
                teamProjectName);
        }

        var command = new ExportReleaseDefinitionCommand(
            execInfo, _OutputProvider);

        await command.ExecuteAsync();

        var releaseDefJson = command.LastResultRawJson;

        return releaseDefJson;
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
        var requestUrl = $"{teamProjectName}/_apis/release/definitions?api-version=7.1";

        var result = await CallEndpointViaGetAndGetResult<GetReleasesForProjectResponse>(
            requestUrl, azureDevOpsUrlTargetType: AzureDevOpsUrlTargetType.Release);

        LastResult = result;

        return result;
    }


}
