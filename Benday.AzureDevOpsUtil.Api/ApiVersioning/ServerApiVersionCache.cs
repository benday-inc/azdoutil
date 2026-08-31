using System.Collections.Concurrent;

namespace Benday.AzureDevOpsUtil.Api.ApiVersioning;

/// <summary>
/// Remembers what each collection can serve, for the life of the process.
///
/// The tool runs as a one-shot command, so this is deliberately not persisted:
/// a stale file claiming an upgraded server still speaks the old version would
/// be worse than the single rejected request that discovery actually costs, and
/// that cost is paid only by collections old enough to reject something.
/// </summary>
public static class ServerApiVersionCache
{
    private static readonly ConcurrentDictionary<string, ServerApiVersionInfo> _Known = new();

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _Gates = new();

    /// <summary>
    /// Collection urls vary by trailing separator and casing between the stored
    /// configuration and the request, and they all mean the same server.
    /// </summary>
    public static string GetKey(string? collectionUrl) =>
        (collectionUrl ?? string.Empty).TrimEnd('/').ToLowerInvariant();

    public static ServerApiVersionInfo? Get(string? collectionUrl)
    {
        return _Known.TryGetValue(GetKey(collectionUrl), out var info) == true ? info : null;
    }

    public static void Set(string? collectionUrl, ServerApiVersionInfo info)
    {
        _Known[GetKey(collectionUrl)] = info;
    }

    /// <summary>
    /// Runs <paramref name="discover"/> at most once per collection even when
    /// several requests are in flight together, so a command that fans out does
    /// not fire a probe per request.
    /// </summary>
    public static async Task<ServerApiVersionInfo> GetOrDiscoverAsync(
        string? collectionUrl,
        Func<Task<ServerApiVersionInfo>> discover)
    {
        var key = GetKey(collectionUrl);

        if (_Known.TryGetValue(key, out var existing) == true)
        {
            return existing;
        }

        var gate = _Gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync();

        try
        {
            if (_Known.TryGetValue(key, out existing) == true)
            {
                return existing;
            }

            var discovered = await discover();

            _Known[key] = discovered;

            return discovered;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Drops everything learned so far.  Exists for tests, which would
    /// otherwise leak one test's server into the next.
    /// </summary>
    public static void Reset()
    {
        _Known.Clear();
        _Gates.Clear();
    }
}
