using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

/// <summary>
/// A changeset reference as returned by GET {project}/_apis/tfvc/changesets.
/// </summary>
public class TfvcChangesetInfo
{
    [JsonPropertyName("changesetId")]
    public int ChangesetId { get; set; }

    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }

    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public TfvcIdentityRef? Author { get; set; }

    [JsonPropertyName("checkedInBy")]
    public TfvcIdentityRef? CheckedInBy { get; set; }

    public string AuthorDisplayName
    {
        get
        {
            if (Author != null && string.IsNullOrWhiteSpace(Author.DisplayName) == false)
            {
                return Author.DisplayName;
            }

            if (CheckedInBy != null && string.IsNullOrWhiteSpace(CheckedInBy.DisplayName) == false)
            {
                return CheckedInBy.DisplayName;
            }

            return string.Empty;
        }
    }
}
