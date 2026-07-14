using System.Globalization;

using Benday.AzureDevOpsUtil.Api.Messages;

namespace Benday.AzureDevOpsUtil.Api.FlowMetrics;

/// <summary>
/// Console-free grouping of completed work items into throughput-by-week
/// buckets. Extracted from GetCycleTimeAndThroughputCommand so the CLI and
/// the MCP tools group data identically.
/// </summary>
public static class ThroughputWeekGrouper
{
    public static DateTime GetMondayOfWeek(DateTime fromDate)
    {
        int diff = (7 + (fromDate.DayOfWeek - DayOfWeek.Monday)) % 7;

        return fromDate.AddDays(-1 * diff).Date;
    }

    /// <summary>
    /// Groups completed items by the Monday of the week they completed in.
    /// Weeks with no completed items do not appear in the result.
    /// </summary>
    public static Dictionary<DateTime, ThroughputIteration> GroupByWeek(
        IEnumerable<WorkItemCycleTimeData> items)
    {
        var groupedByWeek = new Dictionary<DateTime, ThroughputIteration>();

        if (items == null)
        {
            return groupedByWeek;
        }

        foreach (var item in items)
        {
            AddToWeek(groupedByWeek, item);
        }

        return groupedByWeek;
    }

    /// <summary>
    /// Returns the completed-item count for each week of history. Order is not
    /// significant; used as the sample pool for Monte Carlo forecasting.
    /// </summary>
    public static IReadOnlyList<int> GetWeeklyThroughputCounts(
        IEnumerable<WorkItemCycleTimeData> items)
    {
        return GroupByWeek(items).Values
            .Select(x => x.Items.Count)
            .ToList();
    }

    private static void AddToWeek(
        Dictionary<DateTime, ThroughputIteration> groupedByWeek,
        WorkItemCycleTimeData item)
    {
        var dateValueString = item.CompletedDateSK.ToString();

        var completedDate = DateTime.ParseExact(dateValueString, "yyyyMMdd",
            CultureInfo.InvariantCulture);

        var weekOfYear = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
            completedDate, CalendarWeekRule.FirstDay, DayOfWeek.Monday);

        var firstDayOfWeek = GetMondayOfWeek(completedDate);

        if (groupedByWeek.ContainsKey(firstDayOfWeek) == false)
        {
            groupedByWeek.Add(firstDayOfWeek,
                new ThroughputIteration(weekOfYear, firstDayOfWeek));
        }

        groupedByWeek[firstDayOfWeek].Add(item);
    }
}
