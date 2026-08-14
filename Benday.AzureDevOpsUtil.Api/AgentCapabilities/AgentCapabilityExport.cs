using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.AgentCapabilities;

/// <summary>
/// The file produced by <c>exportagentcapabilities</c> and consumed by
/// <c>importagentcapabilities</c>.  It records the collection it came from (for
/// the reader's benefit) and the per-agent user capabilities.
/// </summary>
public class AgentCapabilityExport
{
    /// <summary>
    /// The collection the capabilities were read from.  Informational; import
    /// does not require the target collection to match.
    /// </summary>
    [JsonPropertyName("collectionUrl")]
    public string CollectionUrl { get; set; } = string.Empty;

    [JsonPropertyName("agents")]
    public List<AgentCapabilityRecord> Agents { get; set; } = new();
}
