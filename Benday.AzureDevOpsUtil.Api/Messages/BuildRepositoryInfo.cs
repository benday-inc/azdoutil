using System.Text.Json;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

/// <summary>
/// The repository a build definition pulls source from.
/// </summary>
public class BuildRepositoryInfo
{
    /// <summary>Repository type. TFVC is "TfsVersionControl"; Git is "TfsGit".</summary>
    public const string TypeTfvc = "TfsVersionControl";

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Values here are strings in practice, but the shape is not guaranteed, so
    /// this stays untyped and callers pull out what they recognize.
    /// </summary>
    [JsonPropertyName("properties")]
    public Dictionary<string, JsonElement> Properties { get; set; } = new();

    public bool IsTfvc =>
        string.Equals(Type, TypeTfvc, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The workspace mappings, held as an escaped JSON string inside the
    /// repository properties.  Returns null when the property is absent or is
    /// not a string.
    /// </summary>
    public string? GetTfvcMappingJson()
    {
        foreach (var pair in Properties)
        {
            if (string.Equals(pair.Key, "tfvcMapping", StringComparison.OrdinalIgnoreCase) == false)
            {
                continue;
            }

            if (pair.Value.ValueKind == JsonValueKind.String)
            {
                return pair.Value.GetString();
            }

            return null;
        }

        return null;
    }
}
