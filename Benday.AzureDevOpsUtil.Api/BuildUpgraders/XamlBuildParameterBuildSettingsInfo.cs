using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.BuildUpgraders;

public class XamlBuildParameterBuildSettingsInfo
{
    [JsonPropertyName("MSBuildArguments")]
    public string MSBuildArguments { get; set; } = string.Empty;

    [JsonPropertyName("MSBuildPlatform")]
    public string MSBuildPlatform { get; set; } = string.Empty;

    [JsonPropertyName("PreActionScriptPath")]
    public string PreActionScriptPath { get; set; } = string.Empty;

    [JsonPropertyName("PreActionScriptArguments")]
    public string PreActionScriptArguments { get; set; } = string.Empty;

    [JsonPropertyName("PostActionScriptPath")]
    public string PostActionScriptPath { get; set; } = string.Empty;

    [JsonPropertyName("PostActionScriptArguments")]
    public string PostActionScriptArguments { get; set; } = string.Empty;

    [JsonPropertyName("RunCodeAnalysis")]
    public string RunCodeAnalysis { get; set; } = string.Empty;

}
