using System.Net;
using System.Text;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.AzureDevOpsUtil.Api.Messages.AgentPools;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

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

        return arguments;
    }

    protected override async Task OnExecute()
    {
        // _TeamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);

        var results = await GetResult();

        if (results == null)
        {
            WriteLine(String.Empty);
            WriteLine("No agent pools found");
        }
        else
        {
            WriteLine(String.Empty);

            WriteLine($"Result count: {results.Count}");

            foreach (var item in results.Pools)
            {
                Print(item);

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

        WriteLine();
    }

    private async Task<GetAgentPoolsResponse?> GetResult()
    {
        string requestUrl;
        requestUrl = $"_apis/distributedtask/pools?api-version=7.1-preview.1";

        var result = await CallEndpointViaGetAndGetResult<GetAgentPoolsResponse>(requestUrl);

        LastResult = result;

        return result;
    }
}
