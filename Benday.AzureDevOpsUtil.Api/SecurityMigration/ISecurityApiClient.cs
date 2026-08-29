namespace Benday.AzureDevOpsUtil.Api.SecurityMigration;

/// <summary>
/// The security and identity calls the local-group migration needs.  Services
/// depend on this rather than HTTP, so they run against canned payloads in
/// tests.
/// </summary>
public interface ISecurityApiClient
{
    Task<IReadOnlyList<SecurityNamespaceInfo>> GetSecurityNamespacesAsync();

    /// <summary>Every ACL in the namespace -- no token filter.</summary>
    Task<IReadOnlyList<AccessControlListInfo>> GetAccessControlListsAsync(string namespaceId);

    Task<IReadOnlyList<TfsIdentityInfo>> ReadIdentitiesByDescriptorsAsync(
        IReadOnlyList<string> descriptors, bool includeDirectMembership);

    Task<TfsIdentityInfo?> ReadIdentityByAccountNameAsync(string accountName);

    /// <summary>
    /// Applies ACEs to a token with merge semantics, so existing grants for
    /// other identities are left alone.  Returns false when the server refused.
    /// </summary>
    Task<bool> SetAccessControlEntriesAsync(
        string namespaceId, string token, IReadOnlyList<AccessControlEntryInfo> entries);
}
