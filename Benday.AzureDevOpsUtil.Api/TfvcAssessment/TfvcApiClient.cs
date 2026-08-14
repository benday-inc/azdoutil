using System.Globalization;
using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.Messages;

namespace Benday.AzureDevOpsUtil.Api.TfvcAssessment;

/// <summary>
/// Builds the TFVC request urls and deserializes the responses.  HTTP itself is
/// supplied as a delegate so the command can hand over its authenticated client
/// and tests can hand over canned JSON.
/// </summary>
public class TfvcApiClient : ITfvcApiClient
{
    /// <summary>
    /// Matches the api-version used elsewhere in this tool.  7.0 is what Azure
    /// DevOps Server 2022 supports.
    /// </summary>
    public const string ApiVersion = "7.0";

    /// <summary>
    /// Page size for changeset paging.  The endpoint returns no total count, so
    /// paging continues until a short page comes back or the cap is reached.
    /// </summary>
    public const int ChangesetPageSize = 100;

    private readonly Func<string, Task<string?>> _getJsonAsync;

    /// <param name="getJsonAsync">
    /// Issues a GET against a request url relative to the collection, returning
    /// the response body, or null when the call failed.
    /// </param>
    public TfvcApiClient(Func<string, Task<string?>> getJsonAsync)
    {
        _getJsonAsync = getJsonAsync ?? throw new ArgumentNullException(nameof(getJsonAsync));
    }

    public async Task<IReadOnlyList<TfvcBranchInfo>> GetBranchesAsync(string projectName)
    {
        var requestUrl =
            $"{Uri.EscapeDataString(projectName)}/_apis/tfvc/branches" +
            $"?includeParent=true&includeChildren=true&api-version={ApiVersion}";

        var json = await _getJsonAsync(requestUrl);

        if (string.IsNullOrWhiteSpace(json) == true)
        {
            return Array.Empty<TfvcBranchInfo>();
        }

        var response = JsonSerializer.Deserialize<TfvcBranchListResponse>(


            json, JsonUtilities.DefaultOptions);

        if (response == null)
        {
            return Array.Empty<TfvcBranchInfo>();
        }

        return response.Value;
    }

    public async Task<IReadOnlyList<TfvcItemInfo>> GetItemsAsync(
        string projectName, string scopePath, TfvcRecursionLevel recursionLevel)
    {
        var requestUrl =
            $"{Uri.EscapeDataString(projectName)}/_apis/tfvc/items" +
            $"?scopePath={Uri.EscapeDataString(TfvcPath.Normalize(scopePath))}" +
            $"&recursionLevel={recursionLevel}" +
            $"&api-version={ApiVersion}";

        var json = await _getJsonAsync(requestUrl);

        if (string.IsNullOrWhiteSpace(json) == true)
        {
            return Array.Empty<TfvcItemInfo>();
        }

        var response = JsonSerializer.Deserialize<TfvcItemListResponse>(


            json, JsonUtilities.DefaultOptions);

        if (response == null)
        {
            return Array.Empty<TfvcItemInfo>();
        }

        return response.Value;
    }

    public async Task<IReadOnlyList<TfvcChangesetInfo>> GetChangesetsAsync(
        string projectName, string itemPath, DateTime? fromDateUtc, int maxResults)
    {
        var results = new List<TfvcChangesetInfo>();

        if (maxResults <= 0)
        {
            return results;
        }

        var skip = 0;

        while (results.Count < maxResults)
        {
            var take = Math.Min(ChangesetPageSize, maxResults - results.Count);

            var requestUrl = BuildChangesetsRequestUrl(
                projectName, itemPath, fromDateUtc, take, skip);

            var json = await _getJsonAsync(requestUrl);

            if (string.IsNullOrWhiteSpace(json) == true)
            {
                break;
            }

            var response = JsonSerializer.Deserialize<TfvcChangesetListResponse>(


                json, JsonUtilities.DefaultOptions);

            if (response == null || response.Value.Count == 0)
            {
                break;
            }

            results.AddRange(response.Value);

            // A short page means there is nothing left to read.
            if (response.Value.Count < take)
            {
                break;
            }

            skip += response.Value.Count;
        }

        return results;
    }

    public async Task<string?> GetFileContentAsync(string projectName, string path)
    {
        // $format=text asks for the file itself rather than the json metadata
        // that describes it.
        var requestUrl =
            $"{Uri.EscapeDataString(projectName)}/_apis/tfvc/items" +
            $"?path={Uri.EscapeDataString(TfvcPath.Normalize(path))}" +
            $"&$format=text" +
            $"&api-version={ApiVersion}";

        return await _getJsonAsync(requestUrl);
    }

    public string BuildChangesetsRequestUrl(
        string projectName, string itemPath, DateTime? fromDateUtc, int top, int skip)
    {
        var requestUrl =
            $"{Uri.EscapeDataString(projectName)}/_apis/tfvc/changesets" +
            $"?searchCriteria.itemPath={Uri.EscapeDataString(TfvcPath.Normalize(itemPath))}" +
            $"&$top={top.ToString(CultureInfo.InvariantCulture)}" +
            $"&$skip={skip.ToString(CultureInfo.InvariantCulture)}";

        if (fromDateUtc.HasValue == true)
        {
            // Verified against Azure DevOps: ISO 8601 filters correctly here.
            // The MM-dd-yyyy form the REST samples use works too, but it cannot
            // carry a time and reads as ambiguous outside the US.
            var fromDate = fromDateUtc.Value.ToString(
                "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

            requestUrl += $"&searchCriteria.fromDate={Uri.EscapeDataString(fromDate)}";
        }

        requestUrl += $"&api-version={ApiVersion}";

        return requestUrl;
    }
}
