namespace Benday.AzureDevOpsUtil.Api.AgentCapabilities;

/// <summary>
/// The read/write agent pool calls the capability commands need.  The service
/// depends on this rather than HTTP so it runs against canned pools and agents
/// in tests.  <see cref="AgentPoolClient"/> is the real implementation.
/// </summary>
public interface IAgentPoolClient
{
    Task<IReadOnlyList<AgentPoolSummary>> GetPoolsAsync();

    /// <summary>
    /// The agents in a pool, each with its user-defined capabilities.  The pool
    /// name is passed through so the returned records are self-describing.
    /// </summary>
    Task<IReadOnlyList<AgentCapabilityRecord>> GetAgentsAsync(int poolId, string poolName);

    /// <summary>
    /// Replaces the agent's user capabilities with the supplied set.  The
    /// underlying REST call is a full PUT, so the caller is responsible for
    /// having already merged in anything that should be preserved.
    /// </summary>
    Task UpdateUserCapabilitiesAsync(
        int poolId, int agentId, IReadOnlyDictionary<string, string> userCapabilities);
}
