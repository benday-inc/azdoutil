using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class GetWorkItemByIdResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("rev")]
    public long Rev { get; set; }
    [JsonPropertyName("fields")]
    public Dictionary<string, object> Fields { get; set; } = new();

    [JsonPropertyName("relations")]
    public WorkItemRelation[] Relations { get; set; } = new WorkItemRelation[0];

    [JsonPropertyName("url")]
    public Uri? Url { get; set; }

    private Dictionary<string, string>? _fieldsAsStrings;

    [JsonIgnore]
    public Dictionary<string, string> FieldsAsStrings
    {
        get
        {
            if (_fieldsAsStrings == null)
            {
                _fieldsAsStrings = new Dictionary<string, string>();

                foreach (var key in Fields.Keys)
                {
                    if (Fields[key] != null)
                        _fieldsAsStrings.Add(key, Fields[key]?.ToString() ?? string.Empty);
                }
            }

            return _fieldsAsStrings;
        }
    }
}