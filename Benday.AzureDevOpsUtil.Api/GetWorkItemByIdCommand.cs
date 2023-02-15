using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;
using System.Xml.Linq;

namespace Benday.AzureDevOpsUtil.Api;


[Command(Name = Constants.CommandName_GetWorkItemById,
        Description = "Get work item by id",
        IsAsync = true)]
public class GetWorkItemByIdCommand : AzureDevOpsCommandBase
{
    public GetWorkItemByIdCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    protected override ArgumentCollection GetAvailableArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);
        arguments.AddInt32(Constants.CommandArg_WorkItemId)
            .AsRequired()
            .WithDescription("Work item id");

        return arguments;
    }

    protected override async Task OnExecute()
    {
        WorkItemId = Arguments.GetInt32Value(Constants.CommandArg_WorkItemId);

        await RunWorkItemQuery();
    }


    public int WorkItemId
    {
        get;
        private set;
    }   

    private async Task RunWorkItemQuery()
    {
        var requestUrl = $"_apis/wit/workitems/{WorkItemId}?api-version=6.0&$expand=All";

        WorkItem = await CallEndpointViaGetAndGetResult<GetWorkItemByIdResponse>(requestUrl, true);
    }

    public GetWorkItemByIdResponse? WorkItem { get; private set; }
}