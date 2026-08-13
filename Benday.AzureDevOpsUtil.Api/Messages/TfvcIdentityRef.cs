using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

/// <summary>
/// The identity shape returned by the TFVC REST endpoints (branch owner,
/// changeset author, and so on).
/// </summary>
public class TfvcIdentityRef
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("uniqueName")]
    public string UniqueName { get; set; } = string.Empty;
}
