using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.FlowMetrics;

/// <summary>
/// Console-free cycle time percentile calculations. Shared by the
/// suggest-sle / cycletimeconfidence CLI commands and the MCP tools.
/// </summary>
public static class CycleTimeCalculator
{
    /// <summary>
    /// Returns the cycle time (in days) at the given percentile across the
    /// supplied completed items. For example, percent = 85 answers
    /// "85% of items complete in this many days or less".
    /// </summary>
    public static double GetCycleTimeAtPercentile(
        IReadOnlyList<WorkItemCycleTimeData> items, int percent)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        var index = Utilities.GetIndexForPercentForecast(items.Count, percent);

        if (index < 0)
        {
            throw new KnownException(
                $"Could not calculate a cycle time for {items.Count} items and {percent}%.");
        }

        var cycleTimes = items
            .OrderBy(x => x.CycleTimeDays)
            .Select(x => x.CycleTimeDays)
            .ToArray();

        return Math.Round(cycleTimes[index], 2);
    }

    /// <summary>
    /// Returns the typical delivery window (50th / 85th / 95th percentile cycle
    /// times) plus the number of items the calculation is based on.
    /// </summary>
    public static DeliveryWindow GetDeliveryWindow(
        IReadOnlyList<WorkItemCycleTimeData> items)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        if (items.Count == 0)
        {
            throw new KnownException("No completed items available to calculate a delivery window.");
        }

        return new DeliveryWindow
        {
            ItemCount = items.Count,
            CycleTimeDaysAt50Percent = GetCycleTimeAtPercentile(items, 50),
            CycleTimeDaysAt85Percent = GetCycleTimeAtPercentile(items, 85),
            CycleTimeDaysAt95Percent = GetCycleTimeAtPercentile(items, 95)
        };
    }
}
