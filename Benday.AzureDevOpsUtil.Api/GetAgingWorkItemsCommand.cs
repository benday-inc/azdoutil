using System.Globalization;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Benday.AzureDevOpsUtil.Api;

[Command(
    Category = Constants.Category_FlowMetrics,
    Name = Constants.CommandArgumentNameGetAgingWorkItemsCommand,
        Description = "Get aging in-progress work items",
        IsAsync = true)]
public class GetAgingWorkItemsCommand : AzureDevOpsCommandBase
{
    public GetAgingWorkItemsCommand(
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

        arguments.AddString(Constants.ArgumentNameTeamName)
            .AsNotRequired()
            .WithDescription("Team name");

        return arguments;
    }

    protected override async Task OnExecute()
    {
        _TeamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);

        _HasTeamNameQuery = Arguments.HasValue(Constants.ArgumentNameTeamName);

        if (_HasTeamNameQuery == true)
        {
            _TeamName = Arguments.GetStringValue(Constants.ArgumentNameTeamName);

            await ValidateTeamName();
        }

        await GetData();

        if (IsQuietMode == false)
        {
            if (Data == null || Data.Items == null)
            {
                WriteLine("No data or no work items in progress.");
            }
            else
            {
                WriteLine($"Total in progress items: {Data.Items.Length}");

                foreach (var item in Data.Items.OrderByDescending(x => x.AgeInDays))
                {
                    WriteLine($"{item.AgeInDays.ToString("0.0", CultureInfo.InvariantCulture)} day(s): '{item.Title}' ({item.WorkItemId})");
                }
            }
        }
    }

    private string _TeamProjectName = string.Empty;
    private bool _HasTeamNameQuery;
    private string _TeamName = string.Empty;
    private AreaData? _TeamInfo = null;

    private async Task ValidateTeamName()
    {
        var teamProjectNameUrlEncoded = HttpUtility.UrlEncode(_TeamProjectName);

        // _odata/v4.0-preview/Areas?$select=AreaName,AreaPath,AreaSk,AreaLevel2&$filter=AreaLevel2 eq 'Team 1'

        var requestUrl = $"{Configuration.AnalyticsUrl}/{teamProjectNameUrlEncoded}/_odata/v1.0/Areas?" +
            "$select=AreaName,AreaPath,AreaSk,AreaLevel2&" +
             $"$filter=AreaLevel2 eq '{_TeamName}'";

        var results = await CallEndpointViaGetAndGetResult<GetAreasFromODataResponse>(requestUrl, false);

        if (results == null || results.Items == null || results.Items.Length == 0)
        {
            throw new KnownException(
                $"Could not find team named '{_TeamName}' in project '{_TeamProjectName}'.");
        }
        else if (results.Items.Length > 1)
        {
            throw new KnownException(
                $"Found more than one team named '{_TeamName}' in project '{_TeamProjectName}'.");
        }
        else
        {
            _TeamInfo = results.Items[0];
        }
    }

    private async Task GetData()
    {
        var teamProjectNameUrlEncoded = HttpUtility.UrlEncode(_TeamProjectName);

        string requestUrl;

        if (_HasTeamNameQuery == true)
        {
            if (_TeamInfo == null)
            {
                throw new InvalidOperationException($"Team info was not loaded before query");
            }

            /*
             GET https://azdo2022.benday.com/DefaultCollection/20230601e/_odata/v4.0-preview/WorkItems?$filter=WorkItemType 
            eq 'Product Backlog Item' and StateCategory eq 'InProgress'&$select=Title,WorkItemType,AreaSK,InProgressDate,WorkItemId,ClosedDate,StateCategory&$orderby=InProgressDate desc
             */
            WriteLine($"Getting data for team '{_TeamName}'...");
            requestUrl = $"{Configuration.AnalyticsUrl}/{teamProjectNameUrlEncoded}/_odata/v1.0/WorkItems?" +
                "$select=Title,WorkItemType,AreaSK,InProgressDate,WorkItemId,StateCategory&" +
                "$filter=" +
                HttpUtility.UrlEncode($"WorkItemType eq 'Product Backlog Item' and StateCategory eq 'InProgress' and " +
                $"AreaSK eq {_TeamInfo.AreaSK}");
        }
        else
        {
            WriteLine($"Getting data for team project '{_TeamProjectName}'...");
            requestUrl = $"{Configuration.AnalyticsUrl}/{teamProjectNameUrlEncoded}/_odata/v1.0/WorkItems?" +
                "$select=Title,WorkItemType,AreaSK,InProgressDate,WorkItemId,StateCategory&" +
                "$filter=" +
                HttpUtility.UrlEncode($"WorkItemType eq 'Product Backlog Item' and StateCategory eq 'InProgress'");
        }

        Data = await CallEndpointViaGetAndGetResult<AgingWorkItemDataResponse>(requestUrl, false);
    }

    public AgingWorkItemDataResponse? Data { get; private set; }
}
