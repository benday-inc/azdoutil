namespace Benday.AzureDevOpsUtil.Api.FlowMetrics;

/// <summary>
/// Typical delivery window expressed as cycle time percentiles.
/// </summary>
public class DeliveryWindow
{
    public int ItemCount { get; set; }
    public double CycleTimeDaysAt50Percent { get; set; }
    public double CycleTimeDaysAt85Percent { get; set; }
    public double CycleTimeDaysAt95Percent { get; set; }
}

/// <summary>
/// Result for the "typical delivery window" / cycle time confidence operation.
/// </summary>
public class DeliveryWindowResult
{
    public string TeamProject { get; set; } = string.Empty;
    public string? TeamName { get; set; }
    public int DayRange { get; set; }
    public int ItemCount { get; set; }
    public double CycleTimeDaysAt50Percent { get; set; }
    public double CycleTimeDaysAt85Percent { get; set; }
    public double CycleTimeDaysAt95Percent { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public class WeeklyThroughput
{
    public DateTime WeekStarting { get; set; }
    public int ItemsCompleted { get; set; }
    public double AverageCycleTimeDays { get; set; }
}

/// <summary>
/// Result for the throughput / velocity operation.
/// </summary>
public class ThroughputResult
{
    public string TeamProject { get; set; } = string.Empty;
    public string? TeamName { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public int TotalItemsCompleted { get; set; }
    public int NumberOfWeeks { get; set; }
    public double AverageItemsPerWeek { get; set; }
    public double AverageCycleTimeDays { get; set; }
    public List<WeeklyThroughput> WeeklyBreakdown { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

public class ForecastConfidencePoint
{
    public int ConfidencePercent { get; set; }
    public int Value { get; set; }
}

/// <summary>
/// Result for the "forecast completion date for N items" operation.
/// </summary>
public class DurationForecastResult
{
    public string TeamProject { get; set; } = string.Empty;
    public string? TeamName { get; set; }
    public int ItemCount { get; set; }
    public int DayRange { get; set; }
    public int NumberOfWeeksOfHistory { get; set; }
    public int SimulationCount { get; set; }

    /// <summary>Weeks needed at each confidence level.</summary>
    public List<ForecastConfidencePoint> WeeksByConfidence { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Result for the "forecast items completed in N weeks" operation.
/// </summary>
public class ItemsForecastResult
{
    public string TeamProject { get; set; } = string.Empty;
    public string? TeamName { get; set; }
    public int WeekCount { get; set; }
    public int DayRange { get; set; }
    public int NumberOfWeeksOfHistory { get; set; }
    public int SimulationCount { get; set; }

    /// <summary>Items likely completed at each confidence level.</summary>
    public List<ForecastConfidencePoint> ItemsByConfidence { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

public class AgingWorkItem
{
    public int WorkItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string WorkItemType { get; set; } = string.Empty;
    public double AgeInDays { get; set; }
    public bool IsBeyondTypicalDeliveryWindow { get; set; }
}

/// <summary>
/// Result for the "aging / at-risk work" operation.
/// </summary>
public class AgingWorkResult
{
    public string TeamProject { get; set; } = string.Empty;
    public string? TeamName { get; set; }
    public int InProgressItemCount { get; set; }

    /// <summary>The 85th percentile cycle time used as the "typical" threshold, when available.</summary>
    public double? TypicalDeliveryWindowDays { get; set; }
    public int ItemsBeyondTypicalDeliveryWindow { get; set; }
    public List<AgingWorkItem> Items { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Composite health summary for a project.
/// </summary>
public class ProjectSummaryResult
{
    public string TeamProject { get; set; } = string.Empty;
    public string? TeamName { get; set; }
    public int DayRange { get; set; }

    public DeliveryWindowResult? DeliveryWindow { get; set; }
    public ThroughputResult? Throughput { get; set; }
    public AgingWorkResult? AgingWork { get; set; }
    public string Summary { get; set; } = string.Empty;
}
