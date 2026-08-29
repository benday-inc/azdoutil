namespace Benday.AzureDevOpsUtil.Api.SecurityMigration;

public class LocalGroupImportResult
{
    public bool Preview { get; set; }

    /// <summary>Old account name -> the identity found on the new server.</summary>
    public Dictionary<string, TfsIdentityInfo> ResolvedGroups { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Old account names that did not resolve on the new server.  Usually the
    /// PowerShell script has not been run yet, or the identity sync job has
    /// not noticed the new groups.
    /// </summary>
    public List<string> UnresolvedGroups { get; set; } = new();

    public int AppliedAceCount { get; set; }
    public int TokenCount { get; set; }
    public List<string> FailedTokens { get; set; } = new();
}

/// <summary>
/// Reapplies the exported permission grants on the new server.  Each old local
/// group is re-resolved by name under the new machine, and its ACEs are merged
/// into the same namespace and token they came from.  Tokens survive an
/// attach/upgrade of the collection database unchanged, which is what makes
/// this replay valid.
/// </summary>
public class LocalGroupImportService
{
    private readonly ISecurityApiClient _client;
    private readonly Action<string>? _progress;

    public LocalGroupImportService(ISecurityApiClient client, Action<string>? progress = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _progress = progress;
    }

    public async Task<LocalGroupImportResult> ImportAsync(
        LocalGroupsExportDocument document, string newMachineName, bool preview)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (string.IsNullOrWhiteSpace(newMachineName) == true)
        {
            throw new ArgumentException(
                $"{nameof(newMachineName)} is null or empty.", nameof(newMachineName));
        }

        var result = new LocalGroupImportResult
        {
            Preview = preview
        };

        foreach (var group in document.Groups)
        {
            var newAccountName = $"{newMachineName}\\{group.GroupName}";

            _progress?.Invoke($"Resolving '{newAccountName}'...");

            var identity = await _client.ReadIdentityByAccountNameAsync(newAccountName);

            if (identity == null || string.IsNullOrEmpty(identity.Descriptor) == true)
            {
                result.UnresolvedGroups.Add(group.AccountName);
            }
            else
            {
                result.ResolvedGroups[group.AccountName] = identity;
            }
        }

        // Replay the ACEs grouped by namespace and token so each token is one call
        var entriesByToken = new Dictionary<(string NamespaceId, string Token),
            List<AccessControlEntryInfo>>();

        foreach (var ace in document.AccessControlEntries)
        {
            if (result.ResolvedGroups.TryGetValue(
                    ace.GroupAccountName, out var identity) == false)
            {
                continue;
            }

            var key = (ace.NamespaceId, ace.Token);

            if (entriesByToken.TryGetValue(key, out var list) == false)
            {
                list = new List<AccessControlEntryInfo>();
                entriesByToken[key] = list;
            }

            list.Add(new AccessControlEntryInfo
            {
                Descriptor = identity.Descriptor,
                Allow = ace.Allow,
                Deny = ace.Deny
            });
        }

        result.TokenCount = entriesByToken.Count;

        foreach (var pair in entriesByToken)
        {
            if (preview == true)
            {
                result.AppliedAceCount += pair.Value.Count;

                continue;
            }

            _progress?.Invoke(
                $"Applying {pair.Value.Count} entries to token '{pair.Key.Token}'...");

            var succeeded = await _client.SetAccessControlEntriesAsync(
                pair.Key.NamespaceId, pair.Key.Token, pair.Value);

            if (succeeded == true)
            {
                result.AppliedAceCount += pair.Value.Count;
            }
            else
            {
                result.FailedTokens.Add($"{pair.Key.NamespaceId} {pair.Key.Token}");
            }
        }

        return result;
    }
}
