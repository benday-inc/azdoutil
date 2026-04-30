using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages.TaskGroups;

public class TaskGroupVersion
{
    [JsonPropertyName("major")]
    public int Major { get; set; }

    [JsonPropertyName("minor")]
    public int Minor { get; set; }

    [JsonPropertyName("patch")]
    public int Patch { get; set; }

    [JsonPropertyName("isTest")]
    public bool IsTest { get; set; }

    public override string ToString() => $"{Major}.{Minor}.{Patch}";
}
