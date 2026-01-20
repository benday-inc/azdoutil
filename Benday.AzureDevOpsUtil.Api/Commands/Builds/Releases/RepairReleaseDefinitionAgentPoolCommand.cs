using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Commands.ProjectAdministration;
using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.AzureDevOpsUtil.Api.Messages.AgentPools;
using Benday.AzureDevOpsUtil.Api.Messages.BuildQueues;
using Benday.AzureDevOpsUtil.Api.Messages.ReleaseDefinitionDetails;
using Benday.AzureDevOpsUtil.Api.Messages.Releases;
using Benday.CommandsFramework;
using Benday.JsonUtilities;

namespace Benday.AzureDevOpsUtil.Api.Commands.Builds.Releases;

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

    private string _OutputDir = string.Empty;

    private void PopulateOutputDir()
    {
        var currentDir = System.Environment.CurrentDirectory;

        var outputDir = Path.Combine(currentDir, "output", DateTime.Now.Ticks.ToString());

        if (Directory.Exists(outputDir) == false)
        {
            Directory.CreateDirectory(outputDir);
        }

        _OutputDir = outputDir;
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

        PopulateOutputDir();

        if (allProjects == false)
        {
            await RepairForSingleProject(originalReleaseDefInfos, _TeamProjectName, previewOnly);
        }
        else
        {
            await RepairForAllProjects(originalReleaseDefInfos, previewOnly);
        }

        var outputFileName = $"not-updated_{DateTime.Now.Ticks}.txt";

        var outputFilePath = Path.Combine(_OutputDir, outputFileName);

        if (_notUpdated.Count > 0)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"Count: {_notUpdated.Count}");

            builder.AppendLine("The following build definitions were not updated:");

            foreach (var item in _notUpdated)
            {
                builder.AppendLine(item);
            }

            WriteLine(builder.ToString());

            File.WriteAllText(outputFilePath, builder.ToString());

            WriteLine($"Wrote output log to: {outputFilePath}");
        }
        else
        {
            WriteLine("All items updated");
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

            var alreadyProcessed = new List<string>();
                        
            var count = 0;
            var total = teamProjects.Length;

            foreach (var teamProject in teamProjects)
            {
                count++;
                WriteLine();
                WriteLine();
                WriteLine($"Processing project '{teamProject.Name}' ({count} of {total})...");

                if (alreadyProcessed.Contains(teamProject.Name.ToLower()))
                {
                    WriteLine($"Skipping project '{teamProject.Name}' because it was already processed.");
                    _notUpdated.Add($"Skipping project '{teamProject.Name}' because it was already processed.");

                    continue;
                }
                else
                {
                    await RepairForSingleProject(originalReleaseDefs, teamProject.Name, previewOnly);
                }
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
                try
                {
                    await RepairAgentPoolForReleaseDef(
                        originalReleaseDefs,
                        currentQueues, releaseDefInfo, previewOnly, teamProjectName);
                }
                catch (Exception ex)
                {
                    var message = $"Error processing release definition '{releaseDefInfo.Name}' in project '{teamProjectName}': {ex.Message}";

                    _notUpdated.Add(message);

                    WriteLine(message);
                }
            }
        }
    }

    private async Task<GetBuildQueuesResponse?> GetBuildQueues(string teamProjectName)
    {
        var requestUrl = $"{teamProjectName}/_apis/distributedtask/queues?api-version=7.1";

        var result = await CallEndpointViaGetAndGetResult<GetBuildQueuesResponse>(requestUrl);

        return result;
    }

    private async Task UpdateReleaseDefinition(ReleaseInfo releaseDefInfo, string teamProjectName, string updatedBuildDefJson)
    {
        var teamProjectNameEscaped = HttpUtility.UrlPathEncode(teamProjectName);

        using var client = GetHttpClientInstanceForAzureDevOps(AzureDevOpsUrlTargetType.Release);

        var requestUrl = $"{teamProjectNameEscaped}/_apis/release/definitions/{releaseDefInfo.Id}?api-version=7.1";

        WriteLine($"Sending update request for release definition '{releaseDefInfo.Id}' '{releaseDefInfo.Name}' in '{teamProjectName}'...");

        try
        {
            await SendPutForBodySingleAttempt(client, requestUrl, updatedBuildDefJson, true);

            WriteLine($"Updated release definition '{releaseDefInfo.Id}' '{releaseDefInfo.Name}' in '{teamProjectName}' with new queue ids.");
            WriteLine();
        }
        catch (Exception ex)
        {
            var message = $"Error updating release definition '{releaseDefInfo.Name}' in '{teamProjectName}': {ex.Message}";

            _notUpdated.Add(message);
        }
    }

    private async Task RepairAgentPoolForReleaseDef(
        List<ReleaseQueueInfo> originalReleaseDefs,
        GetBuildQueuesResponse currentQueues,
        ReleaseInfo releaseDefInfo, bool previewOnly, string teamProjectName)
    {
        var projectReleaseDefs = originalReleaseDefs.Where(
            x => string.Equals(x.TeamProjectName, teamProjectName, StringComparison.InvariantCultureIgnoreCase))
            .OrderBy(x => x.ReleaseName)
            .ToList();

        var releaseQueueInfo = projectReleaseDefs
            .Where(x => x.ReleaseId == releaseDefInfo.Id &&
                string.Equals(x.ReleaseName, releaseDefInfo.Name, StringComparison.InvariantCultureIgnoreCase))
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

        var teamProjectDirName = teamProjectName.ToLower().Replace(' ', '-');

        teamProjectDirName = string.Concat(teamProjectDirName.Split(Path.GetInvalidFileNameChars()));

        var outputDirForTeamProject = Path.Combine(_OutputDir, teamProjectDirName);

        if (Directory.Exists(outputDirForTeamProject) == false)
        {
            Directory.CreateDirectory(outputDirForTeamProject);
        }

        var releaseNameCleaned = releaseDefInfo.Name.ToLower().Replace(' ', '-');

        releaseNameCleaned = string.Concat(releaseNameCleaned.Split(Path.GetInvalidFileNameChars()));

        var originalFileName = $"release-def-{releaseNameCleaned}-{releaseDefInfo.Id}.original.json";

        File.WriteAllText(Path.Combine(outputDirForTeamProject, originalFileName), releaseDefJson);

        var updatedFileName = $"release-def-{releaseNameCleaned}-{releaseDefInfo.Id}.updated.json";

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

        File.WriteAllText(Path.Combine(outputDirForTeamProject, updatedFileName), releaseDefJson);

        if (previewOnly == true)
        {
            WriteLine("PREVIEW ONLY: not updating release definition in azure devops");

            if (Arguments.GetBooleanValue(PRINT_JSON_ON_PREVIEW) == true)
            {
                WriteLine(updatedBuildDef);
            }
        }
        else
        {
            await UpdateReleaseDefinition(releaseDefInfo, teamProjectName, updatedBuildDef);
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



        try
        {
            await command.ExecuteAsync();
        }
        catch (Exception ex)
        {
            WriteLine();
            WriteLine();
            WriteLine($"Error getting release definition '{releaseDefInfo.Name}' in project '{teamProjectName}': {ex.Message}");
            WriteLine();
            WriteLine("Retrying in a few seconds...");
            await Task.Delay(5000);
            await command.ExecuteAsync();
        }

        var releaseDefJson = command.LastResultRawJson;

        return releaseDefJson;
    }

    private readonly List<string> _notUpdated = new List<string>();

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
