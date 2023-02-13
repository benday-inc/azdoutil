using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class ModifyWorkItemResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("rev")]
    public int Rev { get; set; }


    [JsonPropertyName("fields")]
    public Dictionary<string, object> Fields { get; set; } = new Dictionary<string, object>();

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
                    _fieldsAsStrings.Add(key, Fields[key].ToString());
                }
            }

            return _fieldsAsStrings;
        }
    }

    [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;

    [JsonIgnore]
    public WorkItemFieldsResponse WorkItemTypeInfo { get; set; } = new();

    [JsonIgnore]
    public string State => FieldsAsStrings["System.State"];
}
