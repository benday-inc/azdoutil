using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class TfvcToGitImportExecuteRequest : TfvcToGitImportRequest
{
    [JsonPropertyName("deleteServiceEndpointAfterImportIsDone")]
    public bool DeleteServiceEndpointAfterImportIsDone { get; set; }
}
