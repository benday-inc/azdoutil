using Benday.AzureDevOpsUtil.Api.AgentCapabilities;

namespace Benday.AzureDevOpsUtil.UnitTests;

/// <summary>
/// In-memory <see cref="IAgentPoolClient"/> so the capability service can be
/// exercised against canned pools and agents, and the PUTs it would send can be
/// inspected.
/// </summary>
public class FakeAgentPoolClient : IAgentPoolClient
{
    private readonly List<AgentPoolSummary> _pools = new();
    private readonly Dictionary<int, List<AgentCapabilityRecord>> _agentsByPool = new();

    public List<(int PoolId, int AgentId, Dictionary<string, string> Capabilities)> Updates { get; } = new();

    public void AddPool(int poolId, string poolName, bool isHosted = false)
    {
        _pools.Add(new AgentPoolSummary { Id = poolId, Name = poolName, IsHosted = isHosted });

        if (_agentsByPool.ContainsKey(poolId) == false)
        {
            _agentsByPool[poolId] = new List<AgentCapabilityRecord>();
        }
    }

    public void AddAgent(
        int poolId, string poolName, int agentId, string agentName,
        params (string Key, string Value)[] capabilities)
    {
        if (_agentsByPool.ContainsKey(poolId) == false)
        {
            _agentsByPool[poolId] = new List<AgentCapabilityRecord>();
        }

        var record = new AgentCapabilityRecord
        {
            PoolId = poolId,
            PoolName = poolName,
            AgentId = agentId,
            AgentName = agentName,
            Enabled = true,
            Status = "online"
        };

        foreach (var pair in capabilities)
        {
            record.UserCapabilities[pair.Key] = pair.Value;
        }

        _agentsByPool[poolId].Add(record);
    }

    public Task<IReadOnlyList<AgentPoolSummary>> GetPoolsAsync()
    {
        return Task.FromResult((IReadOnlyList<AgentPoolSummary>)_pools);
    }

    public Task<IReadOnlyList<AgentCapabilityRecord>> GetAgentsAsync(int poolId, string poolName)
    {
        _agentsByPool.TryGetValue(poolId, out var list);

        return Task.FromResult((IReadOnlyList<AgentCapabilityRecord>)(list ?? new List<AgentCapabilityRecord>()));
    }

    public Task UpdateUserCapabilitiesAsync(
        int poolId, int agentId, IReadOnlyDictionary<string, string> userCapabilities)
    {
        Updates.Add((poolId, agentId,
            new Dictionary<string, string>(userCapabilities, StringComparer.OrdinalIgnoreCase)));

        return Task.CompletedTask;
    }
}
