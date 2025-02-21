using System.Text;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class CreateTeamRequest
{
    [JsonPropertyName("teamName")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("teamDesc")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parentGroupGuid")]
    public string ParentGroupGuid { get; set; } = string.Empty;

    [JsonPropertyName("createArea")]
    public bool CreateArea { get; set; } = true;

    [JsonPropertyName("existingUsersJson")]
    public string ExistingUsersJson { get; set; } = "[]";

    [JsonPropertyName("newUsersJson")]
    public string NewUsersJson { get; set; } = "[]";

    private readonly List<string> _existingUsers = new List<string>();

    public void AddUser(string id)
    {
        _existingUsers.Add(id);

        var builder = new StringBuilder();

        builder.Append("[");

        var isFirst = true;

        foreach (var user in _existingUsers)
        {
            if (isFirst == false)
            {
                builder.Append(",");
            }

            builder.Append($"\"{user}\"");

            isFirst = false;
        }

        builder.Append("]");

        ExistingUsersJson = builder.ToString();
    }
}