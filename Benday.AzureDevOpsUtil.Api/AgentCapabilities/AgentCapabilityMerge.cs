namespace Benday.AzureDevOpsUtil.Api.AgentCapabilities;

/// <summary>
/// Works out the final set of user capabilities to PUT for one agent.  Split out
/// as a pure function because it is the one place the merge-versus-replace
/// decision lives, and it is worth testing on its own.
/// </summary>
public static class AgentCapabilityMerge
{
    /// <summary>
    /// Returns the capabilities that should replace the agent's current set.
    /// </summary>
    /// <param name="existing">What the agent has now.</param>
    /// <param name="incoming">What the caller wants applied.</param>
    /// <param name="replace">
    /// When true, the result is exactly <paramref name="incoming"/> — anything
    /// currently on the agent that is not named is dropped.  When false, the
    /// incoming values are layered on top of the existing ones, so nothing is
    /// removed and re-running is additive.
    /// </param>
    public static Dictionary<string, string> ComputeFinal(
        IReadOnlyDictionary<string, string> existing,
        IReadOnlyDictionary<string, string> incoming,
        bool replace)
    {
        if (replace == true)
        {
            return new Dictionary<string, string>(incoming, StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, string>(existing, StringComparer.OrdinalIgnoreCase);

        foreach (var pair in incoming)
        {
            result[pair.Key] = pair.Value;
        }

        return result;
    }
}
