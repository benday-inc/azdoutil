using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class TfvcToGitImportExecuteRequestParameters
{
    [JsonPropertyName("deleteServiceEndpointAfterImportIsDone")]
    public bool DeleteServiceEndpointAfterImportIsDone { get; set; }

    [JsonPropertyName("gitSource")]
    public object? GitSource { get; set; } = null;

    [JsonPropertyName("tfvcSource")]
    public TfvcToGitImportRequestSourceInfo TfvcSource { get; set; } = new();
}
