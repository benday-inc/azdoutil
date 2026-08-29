namespace Benday.AzureDevOpsUtil.Api.SecurityMigration;

public class SecurityNamespaceInfo
{
    public string NamespaceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<SecurityNamespaceAction> Actions { get; set; } = new();

    public List<string> GetActionNamesForBits(int bits)
    {
        return Actions
            .Where(action => (bits & action.Bit) == action.Bit && action.Bit != 0)
            .Select(action => action.Name)
            .ToList();
    }
}

public class SecurityNamespaceAction
{
    public int Bit { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public class AccessControlListInfo
{
    public string Token { get; set; } = string.Empty;
    public bool InheritPermissions { get; set; }
    public List<AccessControlEntryInfo> Entries { get; set; } = new();
}

public class AccessControlEntryInfo
{
    public string Descriptor { get; set; } = string.Empty;
    public int Allow { get; set; }
    public int Deny { get; set; }
}

/// <summary>
/// An identity as the on-prem identity service reports it, with the
/// $type/$value property-bag noise already stripped away.
/// </summary>
public class TfsIdentityInfo
{
    /// <summary>
    /// TFS-internal group SIDs start with this prefix; anything else in an ACL
    /// is a Windows identity.  Internal group SIDs are instance-specific, so
    /// they never survive a move to a new server.
    /// </summary>
    public const string InternalGroupSidPrefix = "S-1-9-1551374245";

    public string Id { get; set; } = string.Empty;
    public string Descriptor { get; set; } = string.Empty;
    public string ProviderDisplayName { get; set; } = string.Empty;
    public string CustomDisplayName { get; set; } = string.Empty;
    public bool IsContainer { get; set; }
    public List<string> MemberDescriptors { get; set; } = new();

    /// <summary>From the property bag: the account name without the domain.</summary>
    public string Account { get; set; } = string.Empty;

    /// <summary>
    /// From the property bag: the NetBIOS-style qualifier.  For a Windows
    /// identity this is the AD domain or the machine name; for a TFS internal
    /// group it is a vstfs:/// url.
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>From the property bag: 'User' or 'Group'.</summary>
    public string SchemaClassName { get; set; } = string.Empty;

    public string DisplayName =>
        string.IsNullOrEmpty(CustomDisplayName) == false ?
            CustomDisplayName : ProviderDisplayName;

    public string AccountName
    {
        get
        {
            if (string.IsNullOrEmpty(Domain) == true || Domain.StartsWith("vstfs:") == true)
            {
                return Account;
            }

            return $"{Domain}\\{Account}";
        }
    }

    public bool IsInternalGroup
    {
        get
        {
            var separatorIndex = Descriptor.IndexOf(';');

            if (separatorIndex < 0)
            {
                return false;
            }

            return Descriptor
                .Substring(separatorIndex + 1)
                .StartsWith(InternalGroupSidPrefix, StringComparison.OrdinalIgnoreCase);
        }
    }
}
