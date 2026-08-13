using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.Messages;

namespace Benday.AzureDevOpsUtil.Api.TfvcAssessment;

/// <summary>
/// Builds the build definition request urls and deserializes the responses.
/// HTTP is supplied as a delegate, the same arrangement as
/// <see cref="TfvcApiClient"/>.
/// </summary>
public class BuildDefinitionApiClient : IBuildDefinitionApiClient
{
    /// <summary>
    /// Matches the api-version the other build definition calls in this tool
    /// use.  Note this is not the same value as the TFVC endpoints use.
    /// </summary>
    public const string ApiVersion = "7.1";

    private readonly Func<string, Task<string?>> _getJsonAsync;

    public BuildDefinitionApiClient(Func<string, Task<string?>> getJsonAsync)
    {
        _getJsonAsync = getJsonAsync ?? throw new ArgumentNullException(nameof(getJsonAsync));
    }

    public async Task<IReadOnlyList<BuildDefinitionInfo>> GetDefinitionsAsync(string projectName)
    {
        var requestUrl =
            $"{Uri.EscapeDataString(projectName)}/_apis/build/definitions" +
            $"?api-version={ApiVersion}";

        var json = await _getJsonAsync(requestUrl);

        if (string.IsNullOrWhiteSpace(json) == true)
        {
            return Array.Empty<BuildDefinitionInfo>();
        }

        var response = JsonSerializer.Deserialize<BuildDefinitionInfoResponse>(


            json, JsonUtilities.DefaultOptions);

        if (response == null)
        {
            return Array.Empty<BuildDefinitionInfo>();
        }

        return response.Values;
    }

    public async Task<BuildDefinitionDetail?> GetDefinitionAsync(
        string projectName, int definitionId)
    {
        // includeLatestBuilds means the last run date arrives with the
        // definition instead of costing another call per definition.
        var requestUrl =
            $"{Uri.EscapeDataString(projectName)}/_apis/build/definitions/{definitionId}" +
            $"?includeLatestBuilds=true&api-version={ApiVersion}";

        var json = await _getJsonAsync(requestUrl);

        if (string.IsNullOrWhiteSpace(json) == true)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<BuildDefinitionDetail>(

                json, JsonUtilities.DefaultOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
