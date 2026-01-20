using System.Net;
using System.Text;
using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.AzureDevOpsUtil.Api.Messages.AgentPools;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.Builds;

[Command(
    Category = Constants.Category_Builds,
    Name = Constants.CommandArgumentNameListAgentPools,
        Description = "List agent pools",
        IsAsync = true)]
public class ListAgentPoolsCommand : AzureDevOpsCommandBase
{
    public GetAgentPoolsResponse? LastResult { get; private set; }

    public ListAgentPoolsCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);

        arguments.AddBoolean(Constants.CommandArgumentNameWithAgents).
            WithDescription("Get agents in each pool").WithDefaultValue(false).AllowEmptyValue().AsNotRequired();
        arguments.AddBoolean(Constants.CommandArgumentNameToJson).
            WithDescription("Output as JSON").WithDefaultValue(false).AllowEmptyValue().AsNotRequired();

        return arguments;
    }

    protected override async Task OnExecute()
    {
        var withAgents = Arguments.GetBooleanValue(Constants.CommandArgumentNameWithAgents);
        var toJson = Arguments.GetBooleanValue(Constants.CommandArgumentNameToJson);

        var results = await GetAgentPools();

        if (results == null)
        {
            WriteLine(String.Empty);
            WriteLine("No agent pools found");
        }
        else
        {
            
            if (withAgents == true)
            {
                foreach (var item in results.Pools)
                {
                    var agents = await GetAgentsInPool(item.Id);

                    if (agents != null)
                    {
                        item.Agents = agents;
                    }
                }
            }

            if (toJson == false)
            {
                WriteLine(String.Empty);
                WriteLine($"Result count: {results.Count}");

                foreach (var item in results.Pools)
                {
                    Print(item);
                }
            }
            else
            {
                var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
                WriteLine(String.Empty);
                WriteLine(json);
            }
        }
    }
    private void WriteLine(string label, string? value)
    {
        WriteLine($"{label}: {value}");
    }

    private void WriteLine(string label, bool value)
    {
        WriteLine(label, value.ToString());
    }

    private void WriteLine(string label, int value)
    {
        WriteLine(label, value.ToString());
    }


    private void WriteLine(string label, int? value)
    {
        if (value.HasValue == true)
        {
            WriteLine(label, value.Value);
        }
        else
        {
            WriteLine(label, "null");
        }
    }

    private void WriteLine(string label, Owner? value)
    {
        if (value == null)
        {
            WriteLine(label, "null");
        }
        else
        {
            WriteLine(label, value.DisplayName);
        }
    }

    private void Print(AgentPool item)
    {
        WriteLine("***********");
        WriteLine("Name", item.Name);
        WriteLine("Id", item.Id);
        WriteLine("IsHosted", item.IsHosted);
        WriteLine("IsLegacy", item.IsLegacy);
        WriteLine("PoolType", item.PoolType);
        WriteLine("Size", item.Size);
        WriteLine("Options", item.Options);
        WriteLine("CreatedOn", item.CreatedOn);
        WriteLine("AutoProvision", item.AutoProvision);
        WriteLine("AutoUpdate", item.AutoUpdate);
        WriteLine("AutoSize", item.AutoSize);
        WriteLine("TargetSize", item.TargetSize);
        WriteLine("AgentCloudId", item.AgentCloudId);
        WriteLine("CreatedBy", item.CreatedBy);
        WriteLine("Owner", item.Owner);
        WriteLine("Scope", item.Scope);

        if (item.Agents != null)
        {
            WriteLine("Agents.Count", item.Agents.Count);

            var agentNumber = 0;

            foreach (var agent in item.Agents.Value)
            {
                agentNumber++;
                WriteLine($"Agent {agentNumber}");
                WriteLine("\tName", agent.Name);
                WriteLine("\t" +
                    "Id", agent.Id);
                WriteLine("\t" +
                    "Version", agent.Version);
                WriteLine("\t" +
                    "OperatingSystem", agent.OperatingSystem);
                WriteLine("\t" +
                    "Enabled", agent.Enabled);
                WriteLine("\t" +
                    "Status", agent.Status);
                WriteLine("\t" +
                    "ProvisioningState", agent.ProvisioningState);
                WriteLine("\t" +
                    "CreatedOn", agent.CreatedOn);
                WriteLine("\t" +
                    "StatusChangedOn", agent.StatusChangedOn);
                WriteLine("\t" +
                    "MaxParallelism",
                    agent.MaxParallelism);
                WriteLine("Agent.Version", agent.Version);
                WriteLine();
            }
        }
    }

    private async Task<GetAgentsByPoolIdResponse?> GetAgentsInPool(int poolId)
    {
        string requestUrl;
        requestUrl = $"_apis/distributedtask/pools/{poolId}/agents?api-version=7.1-preview.1";

        var result = await CallEndpointViaGetAndGetResult<GetAgentsByPoolIdResponse>(requestUrl);

        return result;
    }


    private async Task<GetAgentPoolsResponse?> GetAgentPools()
    {
        string requestUrl;
        requestUrl = $"_apis/distributedtask/pools?api-version=7.1-preview.1";

        var result = await CallEndpointViaGetAndGetResult<GetAgentPoolsResponse>(requestUrl);

        LastResult = result;

        return result;
    }
}
