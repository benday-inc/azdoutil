namespace Benday.AzureDevOpsUtil.Api.AgentCapabilities;

/// <summary>
/// Console-free orchestrator for the agent capability commands.  It reads the
/// inventory of agents, turns a desired change into a plan of per-agent
/// <see cref="CapabilityPlanItem"/>s, and applies the ones that change.  The
/// commands do the argument parsing and printing; this holds the logic so the
/// same numbers can be exercised in tests against <see cref="IAgentPoolClient"/>.
/// </summary>
public class AgentCapabilityService
{
    private readonly IAgentPoolClient _client;

    public AgentCapabilityService(IAgentPoolClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <summary>
    /// Every agent in every pool (optionally one pool), each with its user
    /// capabilities, ordered by pool then agent name.
    /// </summary>
    public async Task<IReadOnlyList<AgentCapabilityRecord>> GetInventoryAsync(
        string? poolNameFilter = null)
    {
        var pools = await _client.GetPoolsAsync();

        var results = new List<AgentCapabilityRecord>();

        foreach (var pool in pools)
        {
            if (string.IsNullOrWhiteSpace(poolNameFilter) == false &&
                string.Equals(pool.Name, poolNameFilter, StringComparison.OrdinalIgnoreCase) == false)
            {
                continue;
            }

            var agents = await _client.GetAgentsAsync(pool.Id, pool.Name);

            results.AddRange(agents);
        }

        return results
            .OrderBy(x => x.PoolName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.AgentName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Builds the export document from an inventory.  Agents with no user
    /// capabilities are left out — the file exists to carry the custom ones.
    /// </summary>
    public static AgentCapabilityExport BuildExport(
        string collectionUrl, IReadOnlyList<AgentCapabilityRecord> inventory)
    {
        return new AgentCapabilityExport
        {
            CollectionUrl = collectionUrl,
            Agents = inventory
                .Where(x => x.UserCapabilities.Count > 0)
                .OrderBy(x => x.PoolName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.AgentName, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    /// <summary>
    /// Plans applying the same set of capabilities to every supplied target
    /// agent.  This is the bulk "push these onto a pool of fresh agents" path.
    /// </summary>
    public IReadOnlyList<CapabilityPlanItem> PlanSet(
        IEnumerable<AgentCapabilityRecord> targets,
        IReadOnlyDictionary<string, string> incoming,
        bool replace)
    {
        return targets
            .Select(target => BuildPlanItem(target, incoming, replace))
            .ToList();
    }

    /// <summary>
    /// Plans restoring an export onto the current inventory, matching agents by
    /// name.  When an exported agent name occurs in more than one pool, the
    /// exported pool name is used to disambiguate; a record that matches no live
    /// agent (or is ambiguous) is reported as unmatched instead of guessed.
    /// </summary>
    public ImportPlan PlanImport(
        IReadOnlyList<AgentCapabilityRecord> inventory,
        AgentCapabilityExport export,
        bool replace)
    {
        var plan = new ImportPlan();

        foreach (var record in export.Agents)
        {
            var nameMatches = inventory
                .Where(x => string.Equals(x.AgentName, record.AgentName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            AgentCapabilityRecord? target;

            if (nameMatches.Count == 0)
            {
                plan.Unmatched.Add(record);
                continue;
            }
            else if (nameMatches.Count == 1)
            {
                target = nameMatches[0];
            }
            else
            {
                // More than one agent with this name; require the pool to match.
                target = nameMatches.FirstOrDefault(x =>
                    string.Equals(x.PoolName, record.PoolName, StringComparison.OrdinalIgnoreCase));

                if (target == null)
                {
                    plan.Unmatched.Add(record);
                    continue;
                }
            }

            plan.Matched.Add(BuildPlanItem(target, record.UserCapabilities, replace));
        }

        return plan;
    }

    /// <summary>
    /// Writes one plan item's final capabilities to its agent.
    /// </summary>
    public async Task ApplyAsync(CapabilityPlanItem item)
    {
        await _client.UpdateUserCapabilitiesAsync(item.PoolId, item.AgentId, item.Final);
    }

    private static CapabilityPlanItem BuildPlanItem(
        AgentCapabilityRecord target,
        IReadOnlyDictionary<string, string> incoming,
        bool replace)
    {
        var existing = new Dictionary<string, string>(
            target.UserCapabilities, StringComparer.OrdinalIgnoreCase);

        var incomingCopy = new Dictionary<string, string>(
            incoming, StringComparer.OrdinalIgnoreCase);

        var final = AgentCapabilityMerge.ComputeFinal(existing, incomingCopy, replace);

        return new CapabilityPlanItem
        {
            PoolName = target.PoolName,
            PoolId = target.PoolId,
            AgentName = target.AgentName,
            AgentId = target.AgentId,
            Existing = existing,
            Incoming = incomingCopy,
            Final = final,
            Replace = replace
        };
    }
}

/// <summary>
/// The outcome of matching an export against the live inventory: the agents that
/// will be written, and the exported records that had nowhere to land.
/// </summary>
public class ImportPlan
{
    public List<CapabilityPlanItem> Matched { get; } = new();
    public List<AgentCapabilityRecord> Unmatched { get; } = new();
}
