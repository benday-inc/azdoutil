using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class CreateTeamRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("createArea")]
    public bool CreateArea { get; set; } = true;

    [JsonPropertyName("existingUsersJson")]
    public string[] ExistingUsers { get; set; } = new string[] { };


    public void AddUser(string id)
    {
        ExistingUsers = ExistingUsers.Append(id).ToArray();
    }
}