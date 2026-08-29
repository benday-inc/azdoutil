using Benday.AzureDevOpsUtil.Api.SecurityMigration;

namespace Benday.AzureDevOpsUtil.UnitTests;

public class FakeSecurityApiClient : ISecurityApiClient
{
    public List<SecurityNamespaceInfo> Namespaces { get; } = new();

    public Dictionary<string, List<AccessControlListInfo>> AclsByNamespaceId { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, TfsIdentityInfo> IdentitiesByDescriptor { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, TfsIdentityInfo> IdentitiesByAccountName { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public List<(string NamespaceId, string Token, List<AccessControlEntryInfo> Entries)>
        SetAccessControlEntriesCalls { get; } = new();

    public bool SetAccessControlEntriesResult { get; set; } = true;

    public Task<IReadOnlyList<SecurityNamespaceInfo>> GetSecurityNamespacesAsync()
    {
        return Task.FromResult<IReadOnlyList<SecurityNamespaceInfo>>(Namespaces);
    }

    public Task<IReadOnlyList<AccessControlListInfo>> GetAccessControlListsAsync(string namespaceId)
    {
        if (AclsByNamespaceId.TryGetValue(namespaceId, out var acls) == true)
        {
            return Task.FromResult<IReadOnlyList<AccessControlListInfo>>(acls);
        }

        return Task.FromResult<IReadOnlyList<AccessControlListInfo>>(
            new List<AccessControlListInfo>());
    }

    public Task<IReadOnlyList<TfsIdentityInfo>> ReadIdentitiesByDescriptorsAsync(
        IReadOnlyList<string> descriptors, bool includeDirectMembership)
    {
        var results = new List<TfsIdentityInfo>();

        foreach (var descriptor in descriptors)
        {
            if (IdentitiesByDescriptor.TryGetValue(descriptor, out var identity) == true)
            {
                results.Add(identity);
            }
        }

        return Task.FromResult<IReadOnlyList<TfsIdentityInfo>>(results);
    }

    public Task<TfsIdentityInfo?> ReadIdentityByAccountNameAsync(string accountName)
    {
        IdentitiesByAccountName.TryGetValue(accountName, out var identity);

        return Task.FromResult(identity);
    }

    public Task<bool> SetAccessControlEntriesAsync(
        string namespaceId, string token, IReadOnlyList<AccessControlEntryInfo> entries)
    {
        SetAccessControlEntriesCalls.Add((namespaceId, token, entries.ToList()));

        return Task.FromResult(SetAccessControlEntriesResult);
    }
}
