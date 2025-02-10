using System.Net;
using System.Text;
using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.AzureDevOpsUtil.Api.Messages.AgentPools;
using Benday.AzureDevOpsUtil.Api.Messages.BuildQueues;
using Benday.CommandsFramework;
using Benday.JsonUtilities;

namespace Benday.AzureDevOpsUtil.Api;

[Command(
    Category = Constants.Category_Builds,
    Name = Constants.CommandArgumentNameRepairBuildDefAgentPool,
        Description = "Repairs the agent pool setting for the build definitions in a team project or team projects. This is helpful after an on-prem to cloud migration.",
        IsAsync = true)]
public class RepairBuildDefinitionAgentPoolCommand : AzureDevOpsCommandBase
{
    private string _TeamProjectName = string.Empty;
    private const string PRINT_JSON_ON_PREVIEW = "PrintJsonOnPreview";

    public BuildDefinitionInfoResponse? LastResult { get; private set; }

    public RepairBuildDefinitionAgentPoolCommand(
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
            .WithDescription("All builds in all projects in this collection")
            .AsNotRequired();

        arguments.AddBoolean(PRINT_JSON_ON_PREVIEW).AllowEmptyValue().WithDefaultValue(false).AsNotRequired().WithDescription("Print modified json in preview mode");

        arguments.AddBoolean(Constants.ArgumentNamePreviewOnly).WithDescription("Preview only. Do not update build definitions.").AsNotRequired().AllowEmptyValue().WithDefaultValue(false);

        arguments.AddFile(Constants.ArgumentNameOriginalBuildInfo)
            .WithDescription("Build def JSON file from on-prem server. Assumes that pools have been recreated in the cloud using the same name.")
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

        var agentPoolInfoFilePath = Arguments.GetPathToFile(Constants.ArgumentNameOriginalBuildInfo);

        var originalBuildDefInfos = JsonSerializer.Deserialize<List<BuildDefinitionInfo>> (
            File.ReadAllText(agentPoolInfoFilePath));

        if (originalBuildDefInfos == null)
        {
            throw new KnownException("Could not deserialize build def info file.");
        }

        var currentAgentPoolInfo = await GetAgentPools();

        if (currentAgentPoolInfo == null)
        {
            throw new KnownException("Could not get current agent pool info from Azure DevOps.");
        }

        if (allProjects == false)
        {
            await RepairForSingleProject(originalBuildDefInfos, currentAgentPoolInfo, _TeamProjectName, previewOnly);
        }
        else
        {
            await RepairForAllProjects(originalBuildDefInfos, currentAgentPoolInfo, previewOnly);
        }
    }

    private async Task RepairForAllProjects(
        List<BuildDefinitionInfo> originalBuildDefs, 
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
                await RepairForSingleProject(originalBuildDefs, agentPoolInfoCurrent, teamProject.Name, previewOnly);
            }
        }
    }

    private async Task RepairForSingleProject(
        List<BuildDefinitionInfo> originalBuildDefs,
        GetAgentPoolsResponse agentPoolInfoCurrent,
        string teamProjectName, bool previewOnly)
    {
        var results = await GetResult(teamProjectName);

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

            WriteLine($"Result count: {results.Count}");

            foreach (var buildDefInfo in results)
            {
                await RepairAgentPoolForBuildDef(originalBuildDefs, currentQueues, agentPoolInfoCurrent, buildDefInfo, previewOnly);
            }
        }
    }

    private async Task UpdateBuildDefinition(BuildDefinitionInfo buildDefInfo, string updatedBuildDef)
    {
        var requestUrl = $"{buildDefInfo.Project.Name}/_apis/build/definitions/{buildDefInfo.Id}?api-version=7.1";

        _ = await SendPutForBodySingleAttempt(requestUrl, updatedBuildDef, true);

        WriteLine($"Updated build definition '{buildDefInfo.Name}' with new agent pool id.");
        WriteLine();
    }

    private async Task RepairAgentPoolForBuildDef(
        List<BuildDefinitionInfo> originalBuildDefs,
        GetBuildQueuesResponse currentQueues,
        GetAgentPoolsResponse agentPoolInfoCurrent,
        BuildDefinitionInfo buildDefInfo, bool previewOnly)
    {
        var execInfo = ExecutionInfo.GetCloneOfArguments(
             Constants.CommandArgumentNameExportBuildDefinition,
             true);

        execInfo.Arguments.Add(
            Constants.ArgumentNameBuildDefinitionName,
            buildDefInfo.Name);

        var command = new ExportBuildDefinitionCommand(
         execInfo, _OutputProvider);

        await command.ExecuteAsync();

        var buildDefJson = command.LastResultRawJson;

        if (String.IsNullOrWhiteSpace(buildDefJson) == true)
        {
            throw new InvalidOperationException("Could not get build definition JSON.");
        }

        var buildDef = command.LastResult;

        if (buildDef == null)
        {
            throw new InvalidOperationException("Could not get build definition from last result.");
        }        

        var originalBuildDef = originalBuildDefs.Where(x => x.Name == buildDef.Name && x.Project.Name == buildDef.Project.Name).FirstOrDefault();

        if (originalBuildDef == null)
        {
            throw new InvalidOperationException($"Could not find original build definition with name '{buildDef.Name}' in project '{buildDef.Project.Name}'.");
        }

        var originalPoolName = originalBuildDef.Queue.Pool?.Name;
        
        if (string.IsNullOrEmpty(originalPoolName) == true)
        {
            throw new InvalidOperationException($"Could not find original pool with name '{originalPoolName}'.");
        }

        var originalQueueName = originalBuildDef.Queue.Name;

        if (string.IsNullOrEmpty(originalQueueName) == true)
        {
            throw new InvalidOperationException($"Could not find original with name '{originalQueueName}'.");
        }

        var originalPoolId = originalBuildDef.Queue.Pool?.Id ?? -1;

        var currentPool = agentPoolInfoCurrent.Pools
            .Where(x => x.Name.Equals(originalPoolName, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

        if (currentPool == null)
        {
            throw new InvalidOperationException($"Could not find current pool with name '{originalPoolName}'.");
        }

        var currentQueue = currentQueues.Value
           .Where(x => x.Name.Equals(originalQueueName, StringComparison.OrdinalIgnoreCase))
           .FirstOrDefault();

        if (currentQueue == null)
        {
            throw new InvalidOperationException($"Could not find current queue with name '{originalQueueName}'.");
        }

        string updatedBuildDef = UpdatePool(buildDefJson, originalPoolId, currentPool, currentQueue);

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
            await UpdateBuildDefinition(buildDefInfo, updatedBuildDef);
        }
    }

    private string UpdatePool(string buildDefJson, int originalPoolId, AgentPool currentPool, BuildQueueInfo currentQueue)
    {
        var editor = new JsonEditor(buildDefJson, true);

        var name = editor.GetValue("name");

        var jobAuthorizationScope = editor.GetValue("jobAuthorizationScope");

        WriteLine($"Updating pool id from '{originalPoolId}' to '{currentPool.Id}' for build def '{name}' and job authorization scope '{jobAuthorizationScope}'.");

        //editor.SetValue(currentPool.Id, "queue", "pool", "id");
        //editor.SetValue(currentPool.Name, "queue", "pool", "name");
        //editor.SetValue(currentPool.IsHosted, "queue", "pool", "isHosted");

        editor.SetValue(currentQueue.Pool.Id, "queue", "pool", "id");
        editor.SetValue(currentQueue.Pool.Name, "queue", "pool", "name");
        editor.SetValue(currentQueue.Pool.IsHosted, "queue", "pool", "isHosted");

        editor.SetValue(currentQueue.Id, "queue", "id");
        editor.SetValue(currentQueue.Name, "queue", "name");
        
        var json = editor.ToJson(true);

        var invalidJobAuthorizationScope = "\"jobAuthorizationScope\": 0";
        var validJobAuthorizationScope = $"\"jobAuthorizationScope\": \"{jobAuthorizationScope}\"";

        json = json.Replace(invalidJobAuthorizationScope, validJobAuthorizationScope);

        return json;
    }

    private async Task<List<BuildDefinitionInfo>?> GetResult(string teamProjectName)
    {
        string requestUrl;

        if (Arguments.GetBooleanValue(Constants.ArgumentNameXaml) == true)
        {
            WriteLine("** GETTING XAML BUILD DEFINITIONS **");
            requestUrl = $"{teamProjectName}/_apis/build/definitions?api-version=2.2";
        }
        else
        {
            requestUrl = $"{teamProjectName}/_apis/build/definitions?api-version=7.1";
        }

        var result = await CallEndpointViaGetAndGetResult<BuildDefinitionInfoResponse>(requestUrl);

        LastResult = result;

        return result?.Values;
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
