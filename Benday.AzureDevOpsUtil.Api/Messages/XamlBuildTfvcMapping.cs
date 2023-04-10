using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class XamlBuildTfvcMapping
{
    private string? _localPath = string.Empty;

    [JsonPropertyName("serverPath")]
    public string ServerPath { get; set; } = string.Empty;

    [JsonPropertyName("mappingType")]
    public string MappingType { get; set; } = string.Empty;

    [JsonPropertyName("localPath")]
    public string? LocalPath
    {
        get => _localPath;
        set
        {
            if (value == null)
            {
                _localPath = string.Empty;
            }
            else
            {
                _localPath = value;
            }            
        }
    }
}
