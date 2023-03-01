using System;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public partial class BacklogWorkItemTargetInfo
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}

