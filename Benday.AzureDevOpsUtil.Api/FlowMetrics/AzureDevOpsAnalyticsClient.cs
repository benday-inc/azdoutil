using System.Net.Http.Headers;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.FlowMetrics;

/// <summary>
/// Console-free access to the Azure DevOps Analytics OData endpoints used by
/// the flow metrics calculations. Reuses the stored AzureDevOpsConfiguration
/// (URL, PAT / Windows auth) so it does not reinvent authentication, and holds
/// a single reusable HttpClient for the lifetime of the caller (important for
/// the long-lived MCP server process).
/// </summary>
public sealed class AzureDevOpsAnalyticsClient : IDisposable
{
    private const string ProductBacklogItemType = "Product Backlog Item";

    private readonly AzureDevOpsConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public AzureDevOpsAnalyticsClient(AzureDevOpsConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpClient = CreateHttpClient(configuration);
    }

    private static HttpClient CreateHttpClient(AzureDevOpsConfiguration configuration)
    {
        var baseUri = new Uri(configuration.CollectionUrl);

        if (configuration.IsWindowsAuth == true)
        {
            var client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true })
            {
                BaseAddress = baseUri
            };

            return client;
        }
        else
        {
            var client = new HttpClient { BaseAddress = baseUri };

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", configuration.GetTokenBase64Encoded());

            return client;
        }
    }

    private string AnalyticsBaseUrl
    {
        get
        {
            var analyticsUrl = _configuration.AnalyticsUrl;

            if (analyticsUrl.EndsWith("/"))
            {
                analyticsUrl = analyticsUrl[..^1];
            }

            return analyticsUrl;
        }
    }

    private async Task<T?> GetAsync<T>(string requestUrl)
    {
        try
        {
            return await GetSingleAttemptAsync<T>(requestUrl);
        }
        catch
        {
            await Task.Delay(Constants.RetryDelayInMillisecs);

            return await GetSingleAttemptAsync<T>(requestUrl);
        }
    }

    private async Task<T?> GetSingleAttemptAsync<T>(string requestUrl)
    {
        var result = await _httpClient.GetAsync(requestUrl);

        if (result.IsSuccessStatusCode == false)
        {
            throw new InvalidOperationException(
                $"Problem with server call to {requestUrl}. {result.StatusCode} {result.ReasonPhrase}");
        }

        var responseContent = await result.Content.ReadAsStringAsync();

        return JsonUtilities.GetJsonValueAsType<T>(responseContent);
    }

    /// <summary>
    /// Resolves a team name to its area (AreaSK) within a project. Throws a
    /// KnownException when the team cannot be uniquely identified.
    /// </summary>
    public async Task<AreaData> ResolveTeamAreaAsync(string teamProject, string teamName)
    {
        var teamProjectNameUrlEncoded = HttpUtility.UrlEncode(teamProject);

        var requestUrl = $"{AnalyticsBaseUrl}/{teamProjectNameUrlEncoded}/_odata/v1.0/Areas?" +
            "$select=AreaName,AreaPath,AreaSk,AreaLevel2&" +
            $"$filter=AreaLevel2 eq '{teamName}'";

        var results = await GetAsync<GetAreasFromODataResponse>(requestUrl);

        if (results == null || results.Items == null || results.Items.Length == 0)
        {
            throw new KnownException(
                $"Could not find team named '{teamName}' in project '{teamProject}'.");
        }
        else if (results.Items.Length > 1)
        {
            throw new KnownException(
                $"Found more than one team named '{teamName}' in project '{teamProject}'.");
        }

        return results.Items[0];
    }

    /// <summary>
    /// Returns completed Product Backlog Items whose completed date is on or
    /// after now minus <paramref name="dayRange"/> days.
    /// </summary>
    public Task<CycleTimeDataResponse?> GetCompletedItemsAsync(
        string teamProject, int dayRange, AreaData? area = null)
    {
        var startOfRange = DateTime.Now.AddDays(-1 * dayRange).ToString("yyyyMMdd");

        return GetCompletedItemsSinceAsync(teamProject, startOfRange, null, area);
    }

    /// <summary>
    /// Returns completed Product Backlog Items whose completed date falls
    /// within the supplied (inclusive) yyyyMMdd bounds.
    /// </summary>
    public async Task<CycleTimeDataResponse?> GetCompletedItemsSinceAsync(
        string teamProject, string startYyyyMmdd, string? endYyyyMmdd, AreaData? area = null)
    {
        var teamProjectNameUrlEncoded = HttpUtility.UrlEncode(teamProject);

        var filter = $"WorkItemType eq '{ProductBacklogItemType}' and State eq 'Done' and " +
            $"CompletedDateSK ge {startYyyyMmdd}";

        if (string.IsNullOrEmpty(endYyyyMmdd) == false)
        {
            filter += $" and CompletedDateSK le {endYyyyMmdd}";
        }

        if (area != null)
        {
            filter += $" and AreaSK eq {area.AreaSK}";
        }

        var requestUrl = $"{AnalyticsBaseUrl}/{teamProjectNameUrlEncoded}/_odata/v1.0/WorkItems?" +
            "$select=WorkItemId,Title,CycleTimeDays,CompletedDateSK&" +
            "$filter=" + HttpUtility.UrlEncode(filter);

        return await GetAsync<CycleTimeDataResponse>(requestUrl);
    }

    /// <summary>
    /// Returns in-progress Product Backlog Items for the project (or team area).
    /// </summary>
    public async Task<AgingWorkItemDataResponse?> GetInProgressItemsAsync(
        string teamProject, AreaData? area = null)
    {
        var teamProjectNameUrlEncoded = HttpUtility.UrlEncode(teamProject);

        var filter = $"WorkItemType eq '{ProductBacklogItemType}' and StateCategory eq 'InProgress'";

        if (area != null)
        {
            filter += $" and AreaSK eq {area.AreaSK}";
        }

        var requestUrl = $"{AnalyticsBaseUrl}/{teamProjectNameUrlEncoded}/_odata/v1.0/WorkItems?" +
            "$select=Title,WorkItemType,AreaSK,InProgressDate,WorkItemId,StateCategory&" +
            "$filter=" + HttpUtility.UrlEncode(filter);

        return await GetAsync<AgingWorkItemDataResponse>(requestUrl);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
