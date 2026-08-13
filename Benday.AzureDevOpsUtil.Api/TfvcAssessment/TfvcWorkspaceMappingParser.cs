using System.Text.Json;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.TfvcAssessment;

/// <summary>
/// One line of a classic build definition's TFVC workspace.
/// </summary>
public class TfvcWorkspaceMapping
{
    public const string TypeMap = "map";
    public const string TypeCloak = "cloak";

    [JsonPropertyName("serverPath")]
    public string ServerPath { get; set; } = string.Empty;

    /// <summary>
    /// "map" or "cloak".  The casing varies between the REST payload and the
    /// older object model, so comparisons are case-insensitive.
    /// </summary>
    [JsonPropertyName("mappingType")]
    public string MappingType { get; set; } = string.Empty;

    [JsonPropertyName("localPath")]
    public string LocalPath { get; set; } = string.Empty;

    public bool IsMap =>
        string.Equals(MappingType, TypeMap, StringComparison.OrdinalIgnoreCase);

    public bool IsCloak =>
        string.Equals(MappingType, TypeCloak, StringComparison.OrdinalIgnoreCase);
}

internal class TfvcWorkspaceMappingContainer
{
    [JsonPropertyName("mappings")]
    public List<TfvcWorkspaceMapping> Mappings { get; set; } = new();
}

/// <summary>
/// Reads the workspace mappings out of a build definition.  The mappings live
/// in the repository properties as a JSON document held inside a string, so
/// this is a second parse rather than part of the definition's own shape.
/// </summary>
public static class TfvcWorkspaceMappingParser
{
    /// <summary>
    /// Returns the mappings, or an empty list when the value is missing or
    /// cannot be read.  A definition with unreadable mappings is still worth
    /// reporting as a TFVC-connected build, so this does not throw.
    /// </summary>
    public static List<TfvcWorkspaceMapping> Parse(string? tfvcMappingJson)
    {
        if (string.IsNullOrWhiteSpace(tfvcMappingJson) == true)
        {
            return new List<TfvcWorkspaceMapping>();
        }

        try
        {
            var container =
                JsonSerializer.Deserialize<TfvcWorkspaceMappingContainer>(tfvcMappingJson);

            if (container == null)
            {
                return new List<TfvcWorkspaceMapping>();
            }

            return container.Mappings
                .Where(x => string.IsNullOrWhiteSpace(x.ServerPath) == false)
                .ToList();
        }
        catch (JsonException)
        {
            return new List<TfvcWorkspaceMapping>();
        }
    }
}
