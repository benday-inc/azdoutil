using System.Net;
using System.Text;
using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.AzureDevOpsUtil.Api.Messages.AgentPools;
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

        arguments.AddFile(Constants.ArgumentNameAgentPoolInfo)
            .WithDescription("Agent pool info JSON file from on-prem server. Assumes that pools have been recreated in the cloud using the same name.")
            .MustExist()
            .AsRequired();

        arguments.AddBoolean(Constants.ArgumentNamePreviewOnly).WithDescription("Preview only. Do not update build definitions.").AsNotRequired().AllowEmptyValue().WithDefaultValue(false);

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

        var agentPoolInfoFilePath = Arguments.GetPathToFile(Constants.ArgumentNameAgentPoolInfo);

        GetAgentPoolsResponse? originalAgentPoolInfo = null;

        originalAgentPoolInfo = JsonSerializer.Deserialize<GetAgentPoolsResponse>(
            File.ReadAllText(agentPoolInfoFilePath));

        if (originalAgentPoolInfo == null)
        {
            throw new KnownException("Could not deserialize agent pool info file.");
        }

        var currentAgentPoolInfo = await GetAgentPools();

        if (currentAgentPoolInfo == null)
        {
            throw new KnownException("Could not get current agent pool info from Azure DevOps.");
        }

        if (allProjects == false)
        {
            await RepairForSingleProject(originalAgentPoolInfo, currentAgentPoolInfo, _TeamProjectName, previewOnly);
        }
        else
        {
            await RepairForAllProjects(originalAgentPoolInfo, currentAgentPoolInfo, previewOnly);
        }
    }

    private async Task RepairForAllProjects(GetAgentPoolsResponse agentPoolInfoOriginal, GetAgentPoolsResponse agentPoolInfoCurrent, bool previewOnly)
    {
        throw new NotImplementedException();

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
                var result = await GetResult(teamProject.Name);

                if (result != null && result.Count > 0)
                {
                    results.AddRange(result);
                }
            }

            WriteLine(String.Empty);
            WriteLine($"Result count: {results.Count}");

            var groupedResults = results.GroupBy(x => x.Project.Name);

            // order by team project name
            groupedResults = groupedResults.OrderBy(x => x.Key);

            foreach (var groupedResult in groupedResults)
            {
                WriteLine();
                WriteLine("** Team Project: " + groupedResult.Key);

                foreach (var item in groupedResult)
                {
                    WriteLine(ToString(item));
                }
            }

        }

    }

    private async Task RepairForSingleProject(GetAgentPoolsResponse agentPoolInfoOriginal,
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
            WriteLine(String.Empty);

            WriteLine($"Result count: {results.Count}");

            foreach (var buildDefInfo in results)
            {
                await RepairAgentPoolForBuildDef(agentPoolInfoOriginal, agentPoolInfoCurrent, buildDefInfo, previewOnly);
            }
        }
    }
    private async Task RepairAgentPoolForBuildDef(
        GetAgentPoolsResponse agentPoolInfoOriginal,
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

        var currentPoolId = buildDef.Queue.Pool.Id;

        var originalPool = agentPoolInfoOriginal.Pools.Where(x => x.Id == currentPoolId).FirstOrDefault();

        if (originalPool == null)
        {
            throw new InvalidOperationException($"Could not find original pool with id '{currentPoolId}'.");
        }

        var currentPool = agentPoolInfoCurrent.Pools
            .Where(x => x.Name.Equals(originalPool.Name, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

        if (currentPool == null)
        {
            throw new InvalidOperationException($"Could not find current pool with name '{originalPool.Name}'.");
        }

        string updatedBuildDef = UpdatePool(buildDefJson, originalPool, currentPool);

        if (previewOnly == true)
        {
            WriteLine("PREVIEW: updated build definition JSON");
            WriteLine(updatedBuildDef);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private string UpdatePool(string buildDefJson, AgentPool originalPool, AgentPool currentPool)
    {
        var editor = new JsonEditor(buildDefJson, true);

        var currentPoolIdFromJson = editor.GetValueAsInt32("queue", "pool", "id");

        if (currentPoolIdFromJson != originalPool.Id)
        {
            throw new InvalidOperationException($"Original pool id '{originalPool.Id}' does not match current pool id '{currentPoolIdFromJson}'.");
        }

        WriteLine($"Updating pool id from '{originalPool.Id}' to '{currentPool.Id}'.");

        editor.SetValue(currentPool.Id, "queue", "pool", "id");
        
        var json = editor.ToJson(true);

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
