using System.Globalization;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Benday.AzureDevOpsUtil.Api;

[Command(Name = Constants.CommandArgumentNameGetCycleTimeAndThroughput,
        Description = "Get cycle time and throughput data for a team project for a date range",
        IsAsync = true)]
public class GetCycleTimeAndThroughputCommand : AzureDevOpsCommandBase
{
    public GetCycleTimeAndThroughputCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
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

        return arguments;
    }

    protected override async Task OnExecute()
    {
        _NumberOfDaysOfHistory = Arguments.GetInt32Value(Constants.ArgumentNameCycleTimeNumberOfDays);
        _TeamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);

        var now = DateTime.Now;
        _StartOfRange = now.AddDays(-1 * _NumberOfDaysOfHistory);

        await GetData();

        if (Data == null || Data.Items == null)
        {
           
        }
        else
        {
            GroupData();
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

    private void GroupData()
    {
        GroupedByWeek = new Dictionary<DateTime, ThroughputIteration>();

        foreach (var item in Data.Items)
        {
            AddToWeek(item);
        }
    }

    private void WriteThroughputByWeek()
    {       

        WriteLine($"Number of weeks: {GroupedByWeek.Count}");

        var keysOrderedByAscending = GroupedByWeek.Keys.OrderBy(x => x);

        foreach (var key in keysOrderedByAscending)
        {
            WriteThroughputForWeek(GroupedByWeek[key]);
        }
    }

    private void WriteThroughputForWeek(ThroughputIteration throughputIteration)
    {
        WriteLine(string.Empty);
        WriteLine($"* Week Starting {throughputIteration.StartOfWeek.ToShortDateString()}:");
        WriteLine($"Throughput    : {throughputIteration.Items.Count}");
        WriteLine($"Avg Cycle Time: {throughputIteration.AverageCycleTime}");
    }

    private void AddToWeek(WorkItemCycleTimeData item)
    {
        var dateValueString = item.CompletedDateSK.ToString();

        var completedDate = DateTime.ParseExact(dateValueString, "yyyyMMdd",
                CultureInfo.InvariantCulture);

        var weekOfYear = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
            completedDate, CalendarWeekRule.FirstDay, DayOfWeek.Monday);

        var firstDayOfWeek = GetMondayOfWeek(completedDate);

        AddToWeek(firstDayOfWeek, weekOfYear, item, completedDate);
    }

    public static DateTime GetMondayOfWeek(DateTime fromDate)
    {
        int diff = (7 + (fromDate.DayOfWeek - DayOfWeek.Monday)) % 7;

        return fromDate.AddDays(-1 * diff).Date;
    }

    private void AddToWeek(DateTime startOfWeek, int weekOfYear, WorkItemCycleTimeData item, DateTime completedDate)
    {
        if (GroupedByWeek.ContainsKey(startOfWeek) == false)
        {
            GroupedByWeek.Add(startOfWeek, 
                new ThroughputIteration(weekOfYear, startOfWeek));
        }

        var iteration = GroupedByWeek[startOfWeek];

        iteration.Add(item);
    }

    private int _NumberOfDaysOfHistory;
    private string _TeamProjectName;
    private DateTime _StartOfRange;

    private async Task GetData()
    {
        var teamProjectNameUrlEncoded = HttpUtility.UrlEncode(_TeamProjectName);

        var startOfRangeODataFormatted = _StartOfRange.ToString("yyyyMMdd");

        var requestUrl = "https://analytics.dev.azure.com/benday/Metrics%20and%20Dashboards%20Research/_odata/v1.0/WorkItems?" +
            "$select=WorkItemId,Title,CycleTimeDays,CompletedDateSK&" +
            "$filter=" +
            HttpUtility.UrlEncode($"WorkItemType eq 'Product Backlog Item' and State eq 'Done' and CompletedDateSK ge {startOfRangeODataFormatted}");
        
        Data = await CallEndpointViaGetAndGetResult<CycleTimeDataResponse>(requestUrl, false);
    }

    public CycleTimeDataResponse? Data { get; private set; }
    public Dictionary<DateTime, ThroughputIteration> GroupedByWeek { get; private set; } = new();
}
