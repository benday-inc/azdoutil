using System.ComponentModel;

using Benday.AzureDevOpsUtil.Api.FlowMetrics;
using Benday.CommandsFramework;

using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Benday.AzureDevOpsUtil.Api.McpTools;

/// <summary>
/// MCP tools that expose azdoutil's flow metrics calculations through
/// natural-language-friendly, outcome-oriented interfaces. Each tool is a thin
/// adapter over <see cref="FlowMetricsService"/> — no calculation logic lives
/// here.
///
/// Tool descriptions are written in outcome language (delivery windows, "what's
/// stuck", "when will this be done") because the description is what the LLM
/// reads to decide which tool to call.
/// </summary>
[McpServerToolType]
public class DeliveryIntelligenceTools
{
    private readonly FlowMetricsService _flowMetricsService;

    public DeliveryIntelligenceTools(FlowMetricsService flowMetricsService)
    {
        _flowMetricsService = flowMetricsService;
    }

    [McpServerTool(Name = "get_typical_delivery_window")]
    [Description(
        "Get the typical delivery window for completed work items. Returns cycle time " +
        "percentiles (50th, 85th, 95th) showing how long items usually take to complete. " +
        "Use this when someone asks 'how long does stuff usually take?' or 'when will this be done?'")]
    public Task<DeliveryWindowResult> GetTypicalDeliveryWindow(
        [Description("The name of the stored azdoutil configuration to use. Leave blank to use the AZDO_CONFIG_NAME environment variable or the default configuration.")]
        string configName,
        [Description("The Azure DevOps team project name.")]
        string teamProject,
        [Description("How many days of completed-item history to analyze. Defaults to 90.")]
        int dayRange = 90)
    {
        return Guard(() => _flowMetricsService.GetTypicalDeliveryWindowAsync(
            ResolveConfigName(configName), teamProject, dayRange));
    }

    [McpServerTool(Name = "get_throughput")]
    [Description(
        "Get throughput and cycle time data showing how many work items the team completes " +
        "per time period. Use this when someone asks 'how much are we getting done?' or " +
        "'what's our velocity?'")]
    public Task<ThroughputResult> GetThroughput(
        [Description("The name of the stored azdoutil configuration to use. Leave blank to use the AZDO_CONFIG_NAME environment variable or the default configuration.")]
        string configName,
        [Description("The Azure DevOps team project name.")]
        string teamProject,
        [Description("Start of the reporting window, e.g. 2026-01-01.")]
        string startDate,
        [Description("End of the reporting window, e.g. 2026-03-31.")]
        string endDate)
    {
        return Guard(() => _flowMetricsService.GetThroughputAsync(
            ResolveConfigName(configName), teamProject, startDate, endDate));
    }

    [McpServerTool(Name = "forecast_completion_date")]
    [Description(
        "Forecast when a given number of work items will likely be completed using Monte Carlo " +
        "simulation based on historical throughput. Use this when someone asks 'when will these " +
        "N items be done?' or 'how long will the remaining work take?'")]
    public Task<DurationForecastResult> ForecastCompletionDate(
        [Description("The name of the stored azdoutil configuration to use. Leave blank to use the AZDO_CONFIG_NAME environment variable or the default configuration.")]
        string configName,
        [Description("The Azure DevOps team project name.")]
        string teamProject,
        [Description("The number of work items remaining to complete.")]
        int itemCount,
        [Description("How many days of completed-item history to base the forecast on. Defaults to 90.")]
        int dayRange = 90)
    {
        return Guard(() => _flowMetricsService.ForecastCompletionAsync(
            ResolveConfigName(configName), teamProject, itemCount, dayRange));
    }

    [McpServerTool(Name = "forecast_items_in_timeframe")]
    [Description(
        "Forecast how many work items can likely be completed in a given number of weeks using " +
        "Monte Carlo simulation. Use this when someone asks 'how much can we get done in 4 weeks?' " +
        "or 'what can we deliver this quarter?'")]
    public Task<ItemsForecastResult> ForecastItemsInTimeframe(
        [Description("The name of the stored azdoutil configuration to use. Leave blank to use the AZDO_CONFIG_NAME environment variable or the default configuration.")]
        string configName,
        [Description("The Azure DevOps team project name.")]
        string teamProject,
        [Description("The number of weeks into the future to forecast.")]
        int weekCount,
        [Description("How many days of completed-item history to base the forecast on. Defaults to 90.")]
        int dayRange = 90)
    {
        return Guard(() => _flowMetricsService.ForecastItemsInTimeframeAsync(
            ResolveConfigName(configName), teamProject, weekCount, dayRange));
    }

    [McpServerTool(Name = "get_aging_work")]
    [Description(
        "Find work items that are currently in progress and may be stuck or aging beyond the " +
        "team's typical delivery window. Use this when someone asks 'what's stuck?' or 'are any " +
        "items at risk?' or 'what should I be worried about?'")]
    public Task<AgingWorkResult> GetAgingWork(
        [Description("The name of the stored azdoutil configuration to use. Leave blank to use the AZDO_CONFIG_NAME environment variable or the default configuration.")]
        string configName,
        [Description("The Azure DevOps team project name.")]
        string teamProject)
    {
        return Guard(() => _flowMetricsService.GetAgingWorkAsync(
            ResolveConfigName(configName), teamProject));
    }

    [McpServerTool(Name = "get_project_summary")]
    [Description(
        "Get an overall health summary for a project including throughput trends, current work " +
        "in progress, aging items, and the typical delivery window. Use this when someone asks " +
        "'how's the project going?' or 'give me the headlines.'")]
    public Task<ProjectSummaryResult> GetProjectSummary(
        [Description("The name of the stored azdoutil configuration to use. Leave blank to use the AZDO_CONFIG_NAME environment variable or the default configuration.")]
        string configName,
        [Description("The Azure DevOps team project name.")]
        string teamProject,
        [Description("How many days of history to summarize. Defaults to 90.")]
        int dayRange = 90)
    {
        return Guard(() => _flowMetricsService.GetProjectSummaryAsync(
            ResolveConfigName(configName), teamProject, dayRange));
    }

    /// <summary>
    /// Runs a service call and converts azdoutil's user-facing
    /// <see cref="KnownException"/> (e.g. "no such configuration; available
    /// configurations are ...") into an <see cref="McpException"/> so the
    /// message reaches the calling assistant instead of being hidden behind a
    /// generic error.
    /// </summary>
    private static async Task<T> Guard<T>(Func<Task<T>> operation)
    {
        try
        {
            return await operation();
        }
        catch (KnownException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    /// <summary>
    /// Resolves the configuration name from the tool parameter, falling back to
    /// the AZDO_CONFIG_NAME environment variable and then the default
    /// configuration. Passing the resolved value on to the service means a
    /// missing configuration produces an error message that lists the available
    /// configurations.
    /// </summary>
    public static string ResolveConfigName(string? configName)
    {
        if (string.IsNullOrWhiteSpace(configName) == false)
        {
            return configName;
        }

        var fromEnvironment = Environment.GetEnvironmentVariable("AZDO_CONFIG_NAME");

        if (string.IsNullOrWhiteSpace(fromEnvironment) == false)
        {
            return fromEnvironment;
        }

        return Constants.DefaultConfigurationName;
    }
}
