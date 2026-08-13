using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

/// <summary>
/// A reference to a branch by path only.  Used for parent and related branch
/// links in the TFVC branches response.
/// </summary>
public class TfvcShallowBranchRef
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;
}
