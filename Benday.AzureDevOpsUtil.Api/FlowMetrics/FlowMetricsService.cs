using System.Globalization;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.FlowMetrics;

/// <summary>
/// Console-free orchestration of the flow metrics calculations. This is the
/// programmatic entry point reused by the MCP tools: it resolves a stored
/// configuration by name, fetches data via <see cref="AzureDevOpsAnalyticsClient"/>,
/// runs the extracted calculators, and returns structured result objects.
/// </summary>
public class FlowMetricsService
{
    private readonly AzureDevOpsConfigurationManager _configurationManager;

    public FlowMetricsService(AzureDevOpsConfigurationManager? configurationManager = null)
    {
        _configurationManager = configurationManager ?? AzureDevOpsConfigurationManager.Instance;
    }

    private AzureDevOpsConfiguration ResolveConfiguration(string? configName)
    {
        var name = string.IsNullOrWhiteSpace(configName)
            ? Constants.DefaultConfigurationName
            : configName;

        var config = _configurationManager.Get(name);

        if (config == null)
        {
            var available = _configurationManager.GetAll()
                .Select(x => x.Name)
                .ToArray();

            var availableMessage = available.Length == 0
                ? "There are no configurations. Add one with the 'addconfig' command."
                : $"Available configurations: {string.Join(", ", available)}.";

            throw new KnownException(
                $"Could not find a configuration named '{name}'. {availableMessage}");
        }

        return config;
    }

    private AzureDevOpsAnalyticsClient CreateClient(AzureDevOpsConfiguration config)
    {
        return new AzureDevOpsAnalyticsClient(config);
    }

    private async Task<AreaData?> ResolveAreaAsync(
        AzureDevOpsAnalyticsClient client, string teamProject, string? teamName)
    {
        if (string.IsNullOrWhiteSpace(teamName))
        {
            return null;
        }

        return await client.ResolveTeamAreaAsync(teamProject, teamName);
    }

    public async Task<DeliveryWindowResult> GetTypicalDeliveryWindowAsync(
        string configName, string teamProject, int dayRange = 90, string? teamName = null)
    {
        var config = ResolveConfiguration(configName);

        using var client = CreateClient(config);

        var area = await ResolveAreaAsync(client, teamProject, teamName);

        var data = await client.GetCompletedItemsAsync(teamProject, dayRange, area);

        if (data == null || data.Items == null || data.Items.Length == 0)
        {
            throw new KnownException(
                $"No completed work items found for project '{teamProject}' in the last {dayRange} day(s).");
        }

        var window = CycleTimeCalculator.GetDeliveryWindow(data.Items);

        return new DeliveryWindowResult
        {
            TeamProject = teamProject,
            TeamName = teamName,
            DayRange = dayRange,
            ItemCount = window.ItemCount,
            CycleTimeDaysAt50Percent = window.CycleTimeDaysAt50Percent,
            CycleTimeDaysAt85Percent = window.CycleTimeDaysAt85Percent,
            CycleTimeDaysAt95Percent = window.CycleTimeDaysAt95Percent,
            Summary =
                $"Based on {window.ItemCount} completed item(s) over the last {dayRange} day(s): " +
                $"half finish within {window.CycleTimeDaysAt50Percent} day(s), " +
                $"85% within {window.CycleTimeDaysAt85Percent} day(s), " +
                $"and 95% within {window.CycleTimeDaysAt95Percent} day(s)."
        };
    }

    public async Task<ThroughputResult> GetThroughputAsync(
        string configName, string teamProject, string startDate, string endDate, string? teamName = null)
    {
        var config = ResolveConfiguration(configName);

        var start = ParseDate(startDate, nameof(startDate));
        var end = ParseDate(endDate, nameof(endDate));

        using var client = CreateClient(config);

        var area = await ResolveAreaAsync(client, teamProject, teamName);

        var data = await client.GetCompletedItemsSinceAsync(
            teamProject, start.ToString("yyyyMMdd"), end.ToString("yyyyMMdd"), area);

        var result = new ThroughputResult
        {
            TeamProject = teamProject,
            TeamName = teamName,
            StartDate = start.ToString("yyyy-MM-dd"),
            EndDate = end.ToString("yyyy-MM-dd")
        };

        if (data == null || data.Items == null || data.Items.Length == 0)
        {
            result.Summary =
                $"No completed work items found for project '{teamProject}' between " +
                $"{result.StartDate} and {result.EndDate}.";
            return result;
        }

        var groupedByWeek = ThroughputWeekGrouper.GroupByWeek(data.Items);

        result.TotalItemsCompleted = data.Items.Length;
        result.NumberOfWeeks = groupedByWeek.Count;
        result.AverageItemsPerWeek = groupedByWeek.Count == 0
            ? 0
            : Math.Round((double)data.Items.Length / groupedByWeek.Count, 2);
        result.AverageCycleTimeDays = Math.Round(data.Items.Average(x => x.CycleTimeDays), 2);

        foreach (var week in groupedByWeek.Values.OrderBy(x => x.StartOfWeek))
        {
            result.WeeklyBreakdown.Add(new WeeklyThroughput
            {
                WeekStarting = week.StartOfWeek,
                ItemsCompleted = week.Items.Count,
                AverageCycleTimeDays = Math.Round(week.AverageCycleTime, 2)
            });
        }

        result.Summary =
            $"The team completed {result.TotalItemsCompleted} item(s) across " +
            $"{result.NumberOfWeeks} week(s) ({result.AverageItemsPerWeek} item(s) per week on average), " +
            $"with an average cycle time of {result.AverageCycleTimeDays} day(s).";

        return result;
    }

    public async Task<DurationForecastResult> ForecastCompletionAsync(
        string configName, string teamProject, int itemCount, int dayRange = 90, string? teamName = null)
    {
        var config = ResolveConfiguration(configName);

        using var client = CreateClient(config);

        var area = await ResolveAreaAsync(client, teamProject, teamName);

        var data = await client.GetCompletedItemsAsync(teamProject, dayRange, area);

        if (data == null || data.Items == null || data.Items.Length == 0)
        {
            throw new KnownException(
                $"No completed work items found for project '{teamProject}' in the last {dayRange} day(s).");
        }

        var weeklyThroughputs = ThroughputWeekGrouper.GetWeeklyThroughputCounts(data.Items);

        var distribution = MonteCarloForecaster.SimulateWeeksForItemCount(weeklyThroughputs, itemCount);

        var result = new DurationForecastResult
        {
            TeamProject = teamProject,
            TeamName = teamName,
            ItemCount = itemCount,
            DayRange = dayRange,
            NumberOfWeeksOfHistory = weeklyThroughputs.Count,
            SimulationCount = distribution.SimulationCount
        };

        foreach (var confidence in ForecastConfidenceLevels)
        {
            result.WeeksByConfidence.Add(new ForecastConfidencePoint
            {
                ConfidencePercent = confidence,
                Value = distribution.GetWeeksAtConfidence(confidence)
            });
        }

        var likely = result.WeeksByConfidence.First(x => x.ConfidencePercent == 85).Value;

        result.Summary =
            $"Completing {itemCount} item(s) is 85% likely to take {likely} week(s) or less, " +
            $"based on {weeklyThroughputs.Count} week(s) of history and " +
            $"{distribution.SimulationCount} Monte Carlo simulations.";

        return result;
    }

    public async Task<ItemsForecastResult> ForecastItemsInTimeframeAsync(
        string configName, string teamProject, int weekCount, int dayRange = 90, string? teamName = null)
    {
        var config = ResolveConfiguration(configName);

        using var client = CreateClient(config);

        var area = await ResolveAreaAsync(client, teamProject, teamName);

        var data = await client.GetCompletedItemsAsync(teamProject, dayRange, area);

        if (data == null || data.Items == null || data.Items.Length == 0)
        {
            throw new KnownException(
                $"No completed work items found for project '{teamProject}' in the last {dayRange} day(s).");
        }

        var weeklyThroughputs = ThroughputWeekGrouper.GetWeeklyThroughputCounts(data.Items);

        var distribution = MonteCarloForecaster.SimulateItemsInWeeks(weeklyThroughputs, weekCount);

        var result = new ItemsForecastResult
        {
            TeamProject = teamProject,
            TeamName = teamName,
            WeekCount = weekCount,
            DayRange = dayRange,
            NumberOfWeeksOfHistory = weeklyThroughputs.Count,
            SimulationCount = distribution.SimulationCount
        };

        foreach (var confidence in ForecastConfidenceLevels)
        {
            result.ItemsByConfidence.Add(new ForecastConfidencePoint
            {
                ConfidencePercent = confidence,
                Value = distribution.GetItemsAtConfidence(confidence)
            });
        }

        var likely = result.ItemsByConfidence.First(x => x.ConfidencePercent == 85).Value;

        result.Summary =
            $"Over {weekCount} week(s) the team is 85% likely to complete at least {likely} item(s), " +
            $"based on {weeklyThroughputs.Count} week(s) of history and " +
            $"{distribution.SimulationCount} Monte Carlo simulations.";

        return result;
    }

    public async Task<AgingWorkResult> GetAgingWorkAsync(
        string configName, string teamProject, string? teamName = null)
    {
        var config = ResolveConfiguration(configName);

        using var client = CreateClient(config);

        var area = await ResolveAreaAsync(client, teamProject, teamName);

        var inProgress = await client.GetInProgressItemsAsync(teamProject, area);

        var result = new AgingWorkResult
        {
            TeamProject = teamProject,
            TeamName = teamName
        };

        // Try to establish the "typical" delivery window (85th percentile cycle
        // time over the last 90 days) to compare aging items against. If there
        // is no completed history, aging is still reported without a threshold.
        double? typicalWindow = null;

        try
        {
            var completed = await client.GetCompletedItemsAsync(teamProject, 90, area);

            if (completed != null && completed.Items != null && completed.Items.Length > 0)
            {
                typicalWindow = CycleTimeCalculator.GetCycleTimeAtPercentile(completed.Items, 85);
            }
        }
        catch (KnownException)
        {
            // No delivery window available; leave it null.
        }

        result.TypicalDeliveryWindowDays = typicalWindow;

        if (inProgress == null || inProgress.Items == null || inProgress.Items.Length == 0)
        {
            result.Summary = $"There are no in-progress items for project '{teamProject}'.";
            return result;
        }

        foreach (var item in inProgress.Items.OrderByDescending(x => x.AgeInDays))
        {
            var age = Math.Round(item.AgeInDays, 1);
            var beyond = typicalWindow.HasValue && age > typicalWindow.Value;

            if (beyond)
            {
                result.ItemsBeyondTypicalDeliveryWindow++;
            }

            result.Items.Add(new AgingWorkItem
            {
                WorkItemId = item.WorkItemId,
                Title = item.Title,
                WorkItemType = item.WorkItemType,
                AgeInDays = age,
                IsBeyondTypicalDeliveryWindow = beyond
            });
        }

        result.InProgressItemCount = result.Items.Count;

        var windowMessage = typicalWindow.HasValue
            ? $"{result.ItemsBeyondTypicalDeliveryWindow} of them are older than the typical " +
              $"delivery window of {typicalWindow.Value} day(s)"
            : "no typical delivery window is available for comparison";

        result.Summary =
            $"There are {result.InProgressItemCount} in-progress item(s); {windowMessage}.";

        return result;
    }

    public async Task<ProjectSummaryResult> GetProjectSummaryAsync(
        string configName, string teamProject, int dayRange = 90, string? teamName = null)
    {
        // Validate the configuration up-front so a bad config name fails fast.
        ResolveConfiguration(configName);

        var result = new ProjectSummaryResult
        {
            TeamProject = teamProject,
            TeamName = teamName,
            DayRange = dayRange
        };

        var notes = new List<string>();

        try
        {
            result.DeliveryWindow = await GetTypicalDeliveryWindowAsync(
                configName, teamProject, dayRange, teamName);
        }
        catch (KnownException ex)
        {
            notes.Add($"Delivery window unavailable: {ex.Message}");
        }

        try
        {
            var end = DateTime.Now;
            var start = end.AddDays(-1 * dayRange);

            result.Throughput = await GetThroughputAsync(
                configName, teamProject,
                start.ToString("yyyy-MM-dd"), end.ToString("yyyy-MM-dd"), teamName);
        }
        catch (KnownException ex)
        {
            notes.Add($"Throughput unavailable: {ex.Message}");
        }

        try
        {
            result.AgingWork = await GetAgingWorkAsync(configName, teamProject, teamName);
        }
        catch (KnownException ex)
        {
            notes.Add($"Aging work unavailable: {ex.Message}");
        }

        var headline = new List<string>();

        if (result.Throughput != null && result.Throughput.TotalItemsCompleted > 0)
        {
            headline.Add(
                $"{result.Throughput.TotalItemsCompleted} item(s) completed over the last {dayRange} day(s) " +
                $"({result.Throughput.AverageItemsPerWeek}/week)");
        }

        if (result.DeliveryWindow != null)
        {
            headline.Add(
                $"85% of items finish within {result.DeliveryWindow.CycleTimeDaysAt85Percent} day(s)");
        }

        if (result.AgingWork != null)
        {
            headline.Add(
                $"{result.AgingWork.InProgressItemCount} item(s) in progress, " +
                $"{result.AgingWork.ItemsBeyondTypicalDeliveryWindow} aging");
        }

        result.Summary = headline.Count > 0
            ? string.Join("; ", headline) + "."
            : "No flow metrics data is available for this project yet.";

        if (notes.Count > 0)
        {
            result.Summary += " " + string.Join(" ", notes);
        }

        return result;
    }

    private static readonly int[] ForecastConfidenceLevels = { 50, 70, 85, 95 };

    private static DateTime ParseDate(string value, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new KnownException($"'{argumentName}' is required.");
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var parsed) == false)
        {
            throw new KnownException(
                $"'{argumentName}' value '{value}' is not a valid date. Use a format like yyyy-MM-dd.");
        }

        return parsed;
    }
}
