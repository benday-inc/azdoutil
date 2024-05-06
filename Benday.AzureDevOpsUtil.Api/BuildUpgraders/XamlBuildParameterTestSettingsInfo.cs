using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.BuildUpgraders;

public class XamlBuildParameterTestSettingsInfo
{
    [JsonPropertyName("AnalyzeTestImpact")]
    public bool AnalyzeTestImpact { get; set; }

    [JsonPropertyName("DisableTests")]
    public bool DisableTests { get; set; }

    [JsonPropertyName("PreActionScriptPath")]
    public string PreActionScriptPath { get; set; } = string.Empty;

    [JsonPropertyName("PreActionScriptArguments")]
    public string PreActionScriptArguments { get; set; } = string.Empty;

    [JsonPropertyName("PostActionScriptPath")]
    public string PostActionScriptPath { get; set; } = string.Empty;

    [JsonPropertyName("PostActionScriptArguments")]
    public string PostActionScriptArguments { get; set; } = string.Empty;

}
