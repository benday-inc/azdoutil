using System;
using System.Linq;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class IdentityInfo
{

    public string IdentityType { get; set; } = string.Empty;

    public string FriendlyDisplayName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string SubHeader { get; set; } = string.Empty;

    public string TeamFoundationId { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public bool IsWindowsGroup { get; set; }
    public bool IsAadGroup { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string MemberCountText { get; set; } = string.Empty;
    public bool IsTeam { get; set; }
    public bool IsProjectLevel { get; set; }
    public bool RestrictEditingMembership { get; set; }
}
