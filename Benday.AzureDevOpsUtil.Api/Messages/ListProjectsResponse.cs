using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Benday.AzureDevOpsUtil.Api.Messages;
public class ListProjectsResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("value")]
    public TeamProjectInfo[] Projects { get; set; } = new TeamProjectInfo[0];
}
