using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.SecurityMigration;

/// <summary>
/// The serialized output of export-local-groups and the input to
/// import-local-groups.  Everything the new server needs to rebuild the
/// permission grants: the groups, who is in them, and the ACLs that
/// reference them.
/// </summary>
public class LocalGroupsExportDocument
{
    [JsonPropertyName("collectionUrl")]
    public string CollectionUrl { get; set; } = string.Empty;

    [JsonPropertyName("machineName")]
    public string MachineName { get; set; } = string.Empty;

    [JsonPropertyName("exportedAtUtc")]
    public DateTime ExportedAtUtc { get; set; }

    [JsonPropertyName("groups")]
    public List<ExportedLocalGroup> Groups { get; set; } = new();

    [JsonPropertyName("accessControlEntries")]
    public List<ExportedAccessControlEntry> AccessControlEntries { get; set; } = new();
}

public class ExportedLocalGroup
{
    [JsonPropertyName("descriptor")]
    public string Descriptor { get; set; } = string.Empty;

    /// <summary>Account name in DOMAIN\Name form, e.g. APPTIER01\Build Admins.</summary>
    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = string.Empty;

    /// <summary>The group name without the machine prefix.</summary>
    [JsonPropertyName("groupName")]
    public string GroupName { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("members")]
    public List<ExportedGroupMember> Members { get; set; } = new();
}

public class ExportedGroupMember
{
    [JsonPropertyName("descriptor")]
    public string Descriptor { get; set; } = string.Empty;

    /// <summary>Account name in DOMAIN\Name form.</summary>
    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>True when the member is itself a group rather than a user.</summary>
    [JsonPropertyName("isContainer")]
    public bool IsContainer { get; set; }

    /// <summary>
    /// True when the member account lives on the app tier machine rather than
    /// in Active Directory.  These cannot be recreated by adding the same
    /// account on a different machine, so the script calls them out.
    /// </summary>
    [JsonPropertyName("isMachineLocal")]
    public bool IsMachineLocal { get; set; }
}

public class ExportedAccessControlEntry
{
    [JsonPropertyName("namespaceId")]
    public string NamespaceId { get; set; } = string.Empty;

    [JsonPropertyName("namespaceName")]
    public string NamespaceName { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("inheritPermissions")]
    public bool InheritPermissions { get; set; }

    /// <summary>Identity descriptor of the local group this ACE is granted to.</summary>
    [JsonPropertyName("descriptor")]
    public string Descriptor { get; set; } = string.Empty;

    /// <summary>Account name of that group, so the import can re-resolve it by name.</summary>
    [JsonPropertyName("groupAccountName")]
    public string GroupAccountName { get; set; } = string.Empty;

    [JsonPropertyName("allow")]
    public int Allow { get; set; }

    [JsonPropertyName("deny")]
    public int Deny { get; set; }

    /// <summary>The allow bits translated to action names, for human readers.</summary>
    [JsonPropertyName("allowActionNames")]
    public List<string> AllowActionNames { get; set; } = new();

    [JsonPropertyName("denyActionNames")]
    public List<string> DenyActionNames { get; set; } = new();
}
