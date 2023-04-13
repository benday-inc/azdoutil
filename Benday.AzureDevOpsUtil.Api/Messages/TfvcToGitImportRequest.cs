using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class TfvcToGitImportRequest
{
    [JsonPropertyName("gitSource")]
    public object? GitSource { get; set; } = null;

    [JsonPropertyName("tfvcSource")]
    public TfvcToGitImportRequestSourceInfo TfvcSource { get; set; } = new();
}
