using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages.Releases;
public class GetReleasesForProjectResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("value")]
    public ReleaseInfo[] Releases { get; set; } = new ReleaseInfo[0];

}
