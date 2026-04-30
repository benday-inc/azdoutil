using System.Text.Json;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages.TaskGroups;

public class TaskGroupStep
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("alwaysRun")]
    public bool AlwaysRun { get; set; }

    [JsonPropertyName("continueOnError")]
    public bool ContinueOnError { get; set; }

    [JsonPropertyName("condition")]
    public string Condition { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("timeoutInMinutes")]
    public int TimeoutInMinutes { get; set; }

    [JsonPropertyName("retryCountOnTaskFailure")]
    public int RetryCountOnTaskFailure { get; set; }

    [JsonPropertyName("inputs")]
    public Dictionary<string, JsonElement> Inputs { get; set; } = new();

    [JsonPropertyName("environment")]
    public Dictionary<string, JsonElement> Environment { get; set; } = new();

    [JsonPropertyName("task")]
    public TaskGroupStepTaskRef Task { get; set; } = new();
}
