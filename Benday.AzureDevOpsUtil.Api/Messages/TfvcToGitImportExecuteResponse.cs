using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class TfvcToGitImportExecuteResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    public ProjectInfo project { get; set; } = new();

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("remoteUrl")]
    public string RemoteUrl { get; set; } = string.Empty;

    [JsonPropertyName("sshUrl")]
    public string SshUrl { get; set; } = string.Empty;

    [JsonPropertyName("webUrl")]
    public string WebUrl { get; set; } = string.Empty;

    [JsonPropertyName("isDisabled")]
    public bool IsDisabled { get; set; }
}