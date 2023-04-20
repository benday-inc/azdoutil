using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class TfvcToGitImportExecuteRequest
{
    [JsonPropertyName("parameters")]
    public TfvcToGitImportExecuteRequestParameters Parameters { get; set; } = new();
}
