using System.Text;
using System.Text.Json;
using System.Xml.Linq;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(
    Category = Constants.Category_WorkItems,
    Name = Constants.CommandName_RunWorkItemQuery,
        Description = "Run work item query",
        IsAsync = true)]
public class RunWorkItemQueryCommand : AzureDevOpsCommandBase
{
    public RunWorkItemQueryCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);
        arguments.AddString(Constants.ArgumentNameTeamProjectName)
            .AsRequired()
            .WithDescription("Team project name containing the qork item query to run");

        arguments.AddString(Constants.ArgumentNameWorkItemQueryName)
          .AsRequired()
          .WithDescription("Work item query name");

        return arguments;
    }

    protected override async Task OnExecute()
    {
        WorkItemQueryInfo = await GetWorkItemQuery();

        _ = await RunWorkItemQuery(WorkItemQueryInfo.Wiql);

        if (LastResultContent != null)
        {
            WriteLine(LastResultContent);
        }
        else
        {
            WriteLine("No result");
        }
    }

    private async Task<string> RunWorkItemQuery(string query)
    {
        using var client = GetHttpClientInstanceForAzureDevOps();

        var teamProjectName = Arguments.GetStringValue(
            Constants.ArgumentNameTeamProjectName);

        var requestUrl = $"{teamProjectName}/_apis/wit/wiql?api-version=6.0";

        var body = new WorkItemQueryRequestBody
        {
            Query = query
        };

        var requestAsJson = JsonSerializer.Serialize(body);

        var requestContent = new StringContent(
            requestAsJson,
            Encoding.UTF8, "application/json");

        var result = await client.PostAsync(requestUrl, requestContent);

        if (result.IsSuccessStatusCode == false)
        {
            throw new InvalidOperationException($"Problem with server call to {requestUrl}. {result.StatusCode} {result.ReasonPhrase}");
        }

        var responseContent = await result.Content.ReadAsStringAsync();

        LastResultContent = responseContent;
        LastResult = JsonUtilities.GetJsonValueAsType<WorkItemQueryExecutionResult>(responseContent);

        return responseContent;
    }

    public string LastResultContent { get; private set; } = string.Empty;
    public WorkItemQueryExecutionResult? LastResult
    {
        get;
        private set;
    }

    public WorkItemQueryInfo? WorkItemQueryInfo
    {
        get;
        private set;
    }

    private async Task<WorkItemQueryInfo> GetWorkItemQuery()
    {
        var args = ExecutionInfo.GetCloneOfArguments(
            Constants.CommandName_ShowWorkItemQuery, true);
        var command = new ShowWorkItemQueryCommand(args, _OutputProvider);

        await command.ExecuteAsync();

        Utilities.AssertNotNull(command.LastResult, "ShowWorkItemQueryCommand.LastResult");
        Utilities.AssertNotNull(command.LastResult?.Value, "ShowWorkItemQueryCommand.LastResult.value");

        if (command.LastResult!.Value.Length == 0)
        {
            throw new InvalidOperationException($"No result for work item query info.");
        }

        return command.LastResult.Value[0];
    }
}
