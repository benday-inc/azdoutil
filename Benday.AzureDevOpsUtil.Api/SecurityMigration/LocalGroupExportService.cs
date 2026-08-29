namespace Benday.AzureDevOpsUtil.Api.SecurityMigration;

public class LocalGroupExportResult
{
    public LocalGroupsExportDocument Document { get; set; } = new();
    public int NamespacesScanned { get; set; }
    public int AclsScanned { get; set; }

    /// <summary>
    /// Every domain / machine qualifier seen on a Windows identity in the ACLs.
    /// When the export finds no groups for the requested machine name, this
    /// is what tells the user which names actually appear.
    /// </summary>
    public List<string> WindowsDomainsSeen { get; set; } = new();
}

/// <summary>
/// Walks every security namespace's ACLs looking for grants held by Windows
/// groups that live on the app tier machine, then serializes those groups,
/// their memberships, and the grants.  Console-free so it can be tested
/// against a fake client.
/// </summary>
public class LocalGroupExportService
{
    private readonly ISecurityApiClient _client;
    private readonly Action<string>? _progress;

    public LocalGroupExportService(ISecurityApiClient client, Action<string>? progress = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _progress = progress;
    }

    public async Task<LocalGroupExportResult> ExportAsync(string machineName, string collectionUrl)
    {
        if (string.IsNullOrWhiteSpace(machineName) == true)
        {
            throw new ArgumentException(
                $"{nameof(machineName)} is null or empty.", nameof(machineName));
        }

        var result = new LocalGroupExportResult();

        result.Document.CollectionUrl = collectionUrl;
        result.Document.MachineName = machineName;
        result.Document.ExportedAtUtc = DateTime.UtcNow;

        var namespaces = await _client.GetSecurityNamespacesAsync();

        result.NamespacesScanned = namespaces.Count;

        // Gather every ACE in every namespace, keyed by the identity holding it
        var acesByDescriptor =
            new Dictionary<string, List<ExportedAccessControlEntry>>(StringComparer.OrdinalIgnoreCase);

        foreach (var ns in namespaces)
        {
            _progress?.Invoke($"Reading ACLs for namespace '{ns.Name}'...");

            var acls = await _client.GetAccessControlListsAsync(ns.NamespaceId);

            result.AclsScanned += acls.Count;

            foreach (var acl in acls)
            {
                foreach (var entry in acl.Entries)
                {
                    var exported = new ExportedAccessControlEntry
                    {
                        NamespaceId = ns.NamespaceId,
                        NamespaceName = ns.Name,
                        Token = acl.Token,
                        InheritPermissions = acl.InheritPermissions,
                        Descriptor = entry.Descriptor,
                        Allow = entry.Allow,
                        Deny = entry.Deny,
                        AllowActionNames = ns.GetActionNamesForBits(entry.Allow),
                        DenyActionNames = ns.GetActionNamesForBits(entry.Deny)
                    };

                    if (acesByDescriptor.TryGetValue(entry.Descriptor, out var list) == false)
                    {
                        list = new List<ExportedAccessControlEntry>();
                        acesByDescriptor[entry.Descriptor] = list;
                    }

                    list.Add(exported);
                }
            }
        }

        _progress?.Invoke(
            $"Resolving {acesByDescriptor.Count} distinct identities from " +
            $"{result.AclsScanned} ACLs...");

        var identities = await _client.ReadIdentitiesByDescriptorsAsync(
            acesByDescriptor.Keys.ToList(), includeDirectMembership: true);

        var localGroups = new List<TfsIdentityInfo>();

        foreach (var identity in identities)
        {
            if (identity.IsInternalGroup == true)
            {
                continue;
            }

            if (string.IsNullOrEmpty(identity.Domain) == true)
            {
                continue;
            }

            if (result.WindowsDomainsSeen.Contains(
                    identity.Domain, StringComparer.OrdinalIgnoreCase) == false)
            {
                result.WindowsDomainsSeen.Add(identity.Domain);
            }

            var isGroup = identity.IsContainer == true ||
                string.Equals(identity.SchemaClassName, "Group", StringComparison.OrdinalIgnoreCase);

            if (isGroup == true &&
                string.Equals(identity.Domain, machineName, StringComparison.OrdinalIgnoreCase))
            {
                localGroups.Add(identity);
            }
        }

        result.WindowsDomainsSeen.Sort(StringComparer.OrdinalIgnoreCase);

        foreach (var group in localGroups.OrderBy(
            x => x.AccountName, StringComparer.OrdinalIgnoreCase))
        {
            _progress?.Invoke($"Reading members of '{group.AccountName}'...");

            var exportedGroup = new ExportedLocalGroup
            {
                Descriptor = group.Descriptor,
                AccountName = group.AccountName,
                GroupName = group.Account,
                DisplayName = group.DisplayName
            };

            exportedGroup.Members.AddRange(
                await ResolveMembers(group.MemberDescriptors, machineName));

            result.Document.Groups.Add(exportedGroup);

            if (acesByDescriptor.TryGetValue(group.Descriptor, out var aces) == true)
            {
                foreach (var ace in aces)
                {
                    ace.GroupAccountName = group.AccountName;
                }

                result.Document.AccessControlEntries.AddRange(aces);
            }
        }

        return result;
    }

    private async Task<List<ExportedGroupMember>> ResolveMembers(
        IReadOnlyList<string> memberDescriptors, string machineName)
    {
        var results = new List<ExportedGroupMember>();

        if (memberDescriptors.Count == 0)
        {
            return results;
        }

        var members = await _client.ReadIdentitiesByDescriptorsAsync(
            memberDescriptors, includeDirectMembership: false);

        foreach (var member in members.OrderBy(
            x => x.AccountName, StringComparer.OrdinalIgnoreCase))
        {
            results.Add(new ExportedGroupMember
            {
                Descriptor = member.Descriptor,
                AccountName = member.AccountName,
                DisplayName = member.DisplayName,
                IsContainer = member.IsContainer,
                IsMachineLocal = string.Equals(
                    member.Domain, machineName, StringComparison.OrdinalIgnoreCase)
            });
        }

        return results;
    }
}
