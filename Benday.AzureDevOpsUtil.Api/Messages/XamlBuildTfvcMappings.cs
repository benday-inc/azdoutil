using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class XamlBuildTfvcMappings
{
    [JsonPropertyName("mappings")]
    public XamlBuildTfvcMapping[] Mappings { get; set; } = new XamlBuildTfvcMapping[0];
}
