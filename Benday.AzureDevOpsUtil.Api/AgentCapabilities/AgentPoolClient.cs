using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.Messages.AgentPools;

namespace Benday.AzureDevOpsUtil.Api.AgentCapabilities;

/// <summary>
/// Builds the agent pool request urls and deserializes the responses.  HTTP is
/// supplied as delegates so the command can hand over its authenticated client
/// and tests can hand over canned JSON — the same arrangement as
/// <see cref="TfvcAssessment.TfvcApiClient"/>.
/// </summary>
public class AgentPoolClient : IAgentPoolClient
{
    /// <summary>
    /// Matches the api-version <see cref="Commands.Builds.ListAgentPoolsCommand"/>
    /// already uses for pools and agents.  The user capabilities endpoint is a
    /// preview api, which this version covers.
    /// </summary>
    public const string ApiVersion = "7.1-preview.1";

    private readonly Func<string, Task<string?>> _getJsonAsync;
    private readonly Func<string, string, Task> _putJsonAsync;

    /// <param name="getJsonAsync">Issues a GET and returns the body, or null on failure.</param>
    /// <param name="putJsonAsync">Issues a PUT of the supplied body.</param>
    public AgentPoolClient(
        Func<string, Task<string?>> getJsonAsync,
        Func<string, string, Task> putJsonAsync)
    {
        _getJsonAsync = getJsonAsync ?? throw new ArgumentNullException(nameof(getJsonAsync));
        _putJsonAsync = putJsonAsync ?? throw new ArgumentNullException(nameof(putJsonAsync));
    }

    public async Task<IReadOnlyList<AgentPoolSummary>> GetPoolsAsync()
    {
        var requestUrl = $"_apis/distributedtask/pools?api-version={ApiVersion}";

        var json = await _getJsonAsync(requestUrl);

        if (string.IsNullOrWhiteSpace(json) == true)
        {
            return Array.Empty<AgentPoolSummary>();
        }

        var response = JsonSerializer.Deserialize<GetAgentPoolsResponse>(
            json, JsonUtilities.DefaultOptions);

        if (response == null)
        {
            return Array.Empty<AgentPoolSummary>();
        }

        return response.Pools
            .Select(x => new AgentPoolSummary
            {
                Id = x.Id,
                Name = x.Name,
                IsHosted = x.IsHosted
            })
            .ToList();
    }

    public async Task<IReadOnlyList<AgentCapabilityRecord>> GetAgentsAsync(int poolId, string poolName)
    {
        var requestUrl =
            $"_apis/distributedtask/pools/{poolId}/agents" +
            $"?includeCapabilities=true&api-version={ApiVersion}";

        var json = await _getJsonAsync(requestUrl);

        if (string.IsNullOrWhiteSpace(json) == true)
        {
            return Array.Empty<AgentCapabilityRecord>();
        }

        var response = JsonSerializer.Deserialize<GetAgentsByPoolIdResponse>(
            json, JsonUtilities.DefaultOptions);

        if (response == null)
        {
            return Array.Empty<AgentCapabilityRecord>();
        }

        return response.Value
            .Select(agent => new AgentCapabilityRecord
            {
                PoolName = poolName,
                PoolId = poolId,
                AgentName = agent.Name,
                AgentId = agent.Id,
                Enabled = agent.Enabled,
                Status = agent.Status,
                UserCapabilities = new Dictionary<string, string>(
                    agent.UserCapabilities, StringComparer.OrdinalIgnoreCase)
            })
            .ToList();
    }

    public async Task UpdateUserCapabilitiesAsync(
        int poolId, int agentId, IReadOnlyDictionary<string, string> userCapabilities)
    {
        var requestUrl =
            $"_apis/distributedtask/pools/{poolId}/agents/{agentId}/usercapabilities" +
            $"?api-version={ApiVersion}";

        // The endpoint body is a flat name/value object.
        var body = JsonSerializer.Serialize(
            userCapabilities.ToDictionary(x => x.Key, x => x.Value));

        await _putJsonAsync(requestUrl, body);
    }
}
