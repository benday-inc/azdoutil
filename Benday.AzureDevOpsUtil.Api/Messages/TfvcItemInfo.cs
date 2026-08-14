using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

/// <summary>
/// An item (file or folder) as returned by GET {project}/_apis/tfvc/items.
/// </summary>
public class TfvcItemInfo
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Null for files: the server leaves this out rather than sending false.
    /// Compare with "== true" rather than treating null as false by accident.
    /// </summary>
    [JsonPropertyName("isFolder")]
    public bool? IsFolder { get; set; }

    /// <summary>
    /// True when TFVC has this folder registered as a branch.  Lets the folder
    /// scan skip folders that the branches API already accounts for.
    ///
    /// Null when the folder is not a branch, so "not a branch" has to be
    /// tested as "!= true" rather than "== false".
    /// </summary>
    [JsonPropertyName("isBranch")]
    public bool? IsBranch { get; set; }

    /// <summary>
    /// Null for folders, which have no size of their own.
    /// </summary>
    [JsonPropertyName("size")]
    public long? Size { get; set; }

    [JsonPropertyName("changeDate")]
    public DateTime? ChangeDate { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
