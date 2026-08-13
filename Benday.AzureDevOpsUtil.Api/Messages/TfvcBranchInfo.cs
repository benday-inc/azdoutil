using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

/// <summary>
/// A branch as returned by GET {project}/_apis/tfvc/branches.  When the request
/// asks for children, the tree comes back nested through <see cref="Children"/>.
/// </summary>
public class TfvcBranchInfo
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("owner")]
    public TfvcIdentityRef? Owner { get; set; }

    [JsonPropertyName("createdDate")]
    public DateTime? CreatedDate { get; set; }

    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; set; }

    [JsonPropertyName("parent")]
    public TfvcShallowBranchRef? Parent { get; set; }

    [JsonPropertyName("children")]
    public List<TfvcBranchInfo> Children { get; set; } = new();

    [JsonPropertyName("relatedBranches")]
    public List<TfvcShallowBranchRef> RelatedBranches { get; set; } = new();
}
