namespace Benday.AzureDevOpsUtil.Api.AgentCapabilities;

/// <summary>
/// What would happen to one agent if a capability operation ran: what it has
/// now, what is coming in, and the final set that would be written.  The
/// commands print these in preview mode and apply the ones that change.
/// </summary>
public class CapabilityPlanItem
{
    public string PoolName { get; set; } = string.Empty;
    public int PoolId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public int AgentId { get; set; }

    public Dictionary<string, string> Existing { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> Incoming { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> Final { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public bool Replace { get; set; }

    /// <summary>
    /// True when writing <see cref="Final"/> would actually change the agent.
    /// When false, the operation is a no-op and the commands skip the PUT.
    /// </summary>
    public bool WillChange
    {
        get
        {
            if (Existing.Count != Final.Count)
            {
                return true;
            }

            foreach (var pair in Final)
            {
                if (Existing.TryGetValue(pair.Key, out var current) == false)
                {
                    return true;
                }

                if (string.Equals(current, pair.Value, StringComparison.Ordinal) == false)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Keys present in the final set that the agent did not already have.</summary>
    public IReadOnlyList<string> AddedKeys =>
        Final.Keys.Where(k => Existing.ContainsKey(k) == false).OrderBy(k => k).ToList();

    /// <summary>Keys the agent already had whose value the final set changes.</summary>
    public IReadOnlyList<string> ChangedKeys =>
        Final.Keys
            .Where(k => Existing.ContainsKey(k) == true &&
                string.Equals(Existing[k], Final[k], StringComparison.Ordinal) == false)
            .OrderBy(k => k)
            .ToList();

    /// <summary>Keys the agent had that the final set drops (only possible with replace).</summary>
    public IReadOnlyList<string> RemovedKeys =>
        Existing.Keys.Where(k => Final.ContainsKey(k) == false).OrderBy(k => k).ToList();
}
