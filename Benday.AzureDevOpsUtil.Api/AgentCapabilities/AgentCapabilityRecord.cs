using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.AgentCapabilities;

/// <summary>
/// One build agent and the user-defined capabilities it carries.  This is the
/// unit that is listed, exported, and matched by name when reapplying to a new
/// server.  Agent ids differ between servers, so the id is informational only —
/// import and set operations match on <see cref="AgentName"/>.
/// </summary>
public class AgentCapabilityRecord
{
    [JsonPropertyName("poolName")]
    public string PoolName { get; set; } = string.Empty;

    [JsonPropertyName("poolId")]
    public int PoolId { get; set; }

    [JsonPropertyName("agentName")]
    public string AgentName { get; set; } = string.Empty;

    [JsonPropertyName("agentId")]
    public int AgentId { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Only the user-defined capabilities.  System capabilities are discovered
    /// by the agent software from its own machine, so transplanting them onto a
    /// different agent would be meaningless.
    /// </summary>
    [JsonPropertyName("userCapabilities")]
    public Dictionary<string, string> UserCapabilities { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}
