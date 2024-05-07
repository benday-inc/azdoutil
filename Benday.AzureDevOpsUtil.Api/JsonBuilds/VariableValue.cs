using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.JsonBuilds;

public class VariableValue
{
    [JsonPropertyName("value")]
    public string? Value { get; set; } = string.Empty;

    [JsonPropertyName("isSecret")]
    public bool IsSecret { get; set; }

    [JsonPropertyName("allowOverride")]
    public bool AllowOverride { get; set; }
}



