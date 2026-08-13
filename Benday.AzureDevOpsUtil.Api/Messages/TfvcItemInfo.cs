using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

/// <summary>
/// An item (file or folder) as returned by GET {project}/_apis/tfvc/items.
/// </summary>
public class TfvcItemInfo
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("isFolder")]
    public bool IsFolder { get; set; }

    /// <summary>
    /// True when TFVC has this folder registered as a branch.  Lets the folder
    /// scan skip folders that the branches API already accounts for.
    /// </summary>
    [JsonPropertyName("isBranch")]
    public bool IsBranch { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("changeDate")]
    public DateTime? ChangeDate { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
