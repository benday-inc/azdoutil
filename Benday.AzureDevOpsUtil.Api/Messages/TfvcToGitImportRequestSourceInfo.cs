using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class TfvcToGitImportRequestSourceInfo
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("importHistory")]
    public bool ImportHistory { get; set; }

    [JsonPropertyName("importHistoryDurationInDays")]
    public int ImportHistoryDurationInDays { get; set; }
}
