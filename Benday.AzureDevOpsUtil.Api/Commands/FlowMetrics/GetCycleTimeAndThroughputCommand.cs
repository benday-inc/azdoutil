using System.Web;

using Benday.AzureDevOpsUtil.Api.FlowMetrics;
using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.FlowMetrics;

[Command(
    Category = Constants.Category_FlowMetrics,
    Name = Constants.CommandArgumentNameGetCycleTimeAndThroughput,
        Description = "Get cycle time and throughput data for a team project for a date range")]
public class GetCycleTimeAndThroughputCommand : AzureDevOpsCommandBase
{
    public GetCycleTimeAndThroughputCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
        _TeamName = string.Empty;
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);
        arguments.AddInt32(Constants.ArgumentNameCycleTimeNumberOfDays)
            .AsRequired()
            .WithDescription("Number of days of history to compute");
        arguments.AddString(Constants.ArgumentNameTeamProjectName)
            .AsRequired()
            .WithDescription("Team project name");

        arguments.AddString(Constants.ArgumentNameTeamName)
            .AsNotRequired()
            .WithDescription("Team name");        

        return arguments;
    }

    protected override async Task OnExecute(CancellationToken cancellationToken)
    {
        _NumberOfDaysOfHistory = Arguments.GetInt32Value(Constants.ArgumentNameCycleTimeNumberOfDays);
        _TeamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);

        _HasTeamNameQuery= Arguments.HasValue(Constants.ArgumentNameTeamName);

        if (_HasTeamNameQuery == true)
        {
            _TeamName = Arguments.GetStringValue(Constants.ArgumentNameTeamName);

            await ValidateTeamName();
        }        

        var now = DateTime.Now;
        _StartOfRange = now.AddDays(-1 * _NumberOfDaysOfHistory);

        await GetData();

        if (Data != null && Data.Items != null)
        {
            GroupedByWeek = ThroughputWeekGrouper.GroupByWeek(Data.Items);
        }

        if (IsQuietMode == false)
        {
            WriteLine($"Number of days: {(now - _StartOfRange).TotalDays}");

            if (Data == null || Data.Items == null)
            {
                WriteLine("No data.");
            }
            else
            {
                WriteLine($"Total throughput: {Data.Items.Length}");

                WriteThroughputByWeek();
            }
        }
    }

    private void WriteThroughputByWeek()
    {
        WriteLine(string.Empty);
        WriteLine($"Throughput for the last {GroupedByWeek.Count} week(s):");

        var keysOrderedByAscending = GroupedByWeek.Keys.OrderBy(x => x);

        foreach (var key in keysOrderedByAscending)
        {
            WriteThroughputForWeek(GroupedByWeek[key]);
        }

        
    }

    private void WriteThroughputForWeek(ThroughputIteration throughputIteration)
    {
        string dateString = throughputIteration.StartOfWeek.ToShortDateString();

        WriteLine($"Week of {dateString}:");
        WriteLine($"\tThroughput    : {throughputIteration.Items.Count} item(s)");
        WriteLine($"\tAvg Cycle Time: {throughputIteration.AverageCycleTime} day(s)");

        WriteLine(string.Empty);
    }

    private int _NumberOfDaysOfHistory;
    private string _TeamProjectName = string.Empty;
    private DateTime _StartOfRange;
    private bool _HasTeamNameQuery;
    private string _TeamName;
    private AreaData? _TeamInfo = null;

    private async Task ValidateTeamName()
    {
        var teamProjectNameUrlEncoded = HttpUtility.UrlEncode(_TeamProjectName);

        var startOfRangeODataFormatted = _StartOfRange.ToString("yyyyMMdd");

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

        var startOfRangeODataFormatted = _StartOfRange.ToString("yyyyMMdd");

        string requestUrl;

        if (_HasTeamNameQuery == true)
        {
            if (_TeamInfo == null)
            {
                throw new InvalidOperationException($"Team info was not loaded before query");
            }

            requestUrl = $"{Configuration.AnalyticsUrl}/{teamProjectNameUrlEncoded}/_odata/v1.0/WorkItems?" +
                "$select=WorkItemId,Title,CycleTimeDays,CompletedDateSK&" +
                "$filter=" +
                HttpUtility.UrlEncode($"WorkItemType eq 'Product Backlog Item' and State eq 'Done' and " +
                $"CompletedDateSK ge {startOfRangeODataFormatted} and " +
                $"AreaSK eq {_TeamInfo.AreaSK}");
        }
        else
        {
            requestUrl = $"{Configuration.AnalyticsUrl}/{teamProjectNameUrlEncoded}/_odata/v1.0/WorkItems?" +
                "$select=WorkItemId,Title,CycleTimeDays,CompletedDateSK&" +
                "$filter=" +
                HttpUtility.UrlEncode($"WorkItemType eq 'Product Backlog Item' and State eq 'Done' and CompletedDateSK ge {startOfRangeODataFormatted}");
        }

        Data = await CallEndpointViaGetAndGetResult<CycleTimeDataResponse>(requestUrl, false);
    }

    public CycleTimeDataResponse? Data { get; private set; }
    public Dictionary<DateTime, ThroughputIteration> GroupedByWeek { get; private set; } = new();
}
