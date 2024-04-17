using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class XamlBuildRepositoryProperties
{
    [JsonPropertyName("tfvcMapping")] 
    public string TfvcMapping { get; set; } = string.Empty;

    private TfvcSourceMappings? _TfvcMappings;

    [JsonIgnore]
    public TfvcSourceMappings TfvcMappings 
    {
        get
        {
            if (_TfvcMappings == null)
            {
                var temp = GetTfvcMappings();

                _TfvcMappings = temp;
            }

            return _TfvcMappings;
        }
    }
    private TfvcSourceMappings GetTfvcMappings()
    {
        if (string.IsNullOrWhiteSpace(TfvcMapping) == true)
        {
            return new TfvcSourceMappings();
        }
        else
        {
            var temp = System.Text.Json.JsonSerializer.Deserialize<TfvcSourceMappings>(TfvcMapping);

            if (temp == null)
            {
                return new TfvcSourceMappings();
            }

            return temp;
        }
    }
}
