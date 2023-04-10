using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class XamlBuildDefinitionDetail
{
    [JsonPropertyName("controller")]
    public XamlBuildDefinitionController Controller { get; set; } = new();

    [JsonPropertyName("batchSize")]
    public int BatchSize { get; set; }

    [JsonPropertyName("triggerType")]
    public string TriggerType { get; set; } = string.Empty;

    [JsonPropertyName("defaultDropLocation")]
    public string DefaultDropLocation { get; set; } = string.Empty;

    [JsonPropertyName("buildArgs")]
    public string BuildArgs { get; set; } = string.Empty;

    [JsonPropertyName("createdOn")]
    public DateTime CreatedOn { get; set; }

    [JsonPropertyName("supportedReasons")]
    public int SupportedReasons { get; set; }

    [JsonPropertyName("lastBuild")]
    public XamlBuildLastBuildInfo LastBuild { get; set; } = new();

    [JsonPropertyName("repository")]
    public XamlBuildRepositoryInfo Repository { get; set; } = new();

    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string BuildType { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("project")]
    public XamlBuildProjectInfo Project { get; set; } = new();
}
