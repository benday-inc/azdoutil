using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

/// <summary>
/// A build definition with the detail that only comes back from the
/// per-definition endpoint.  The definitions list returns shallow objects
/// without the repository, so anything that needs workspace mappings has to
/// fetch each definition on its own.
/// </summary>
public class BuildDefinitionDetail
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("repository")]
    public BuildRepositoryInfo? Repository { get; set; }

    /// <summary>
    /// Populated when the request asks for includeLatestBuilds, which avoids a
    /// separate call per definition just to find out when it last ran.
    /// </summary>
    [JsonPropertyName("latestCompletedBuild")]
    public BuildRunInfo? LatestCompletedBuild { get; set; }

    [JsonPropertyName("latestBuild")]
    public BuildRunInfo? LatestBuild { get; set; }
}
