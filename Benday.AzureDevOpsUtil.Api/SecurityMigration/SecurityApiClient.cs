using System.Text.Json;

namespace Benday.AzureDevOpsUtil.Api.SecurityMigration;

/// <summary>
/// Builds the security and identity request urls and deserializes the
/// responses.  HTTP itself is supplied as delegates so the command can hand
/// over its authenticated client and tests can hand over canned JSON.
///
/// On-prem these endpoints live directly on the collection url -- there is no
/// vssps host outside the cloud service.
/// </summary>
public class SecurityApiClient : ISecurityApiClient
{
    /// <summary>
    /// The newest api-version Azure DevOps Server 2019 accepts.  This module
    /// exists for a TFS 2019 upgrade, and newer servers accept 5.0 too.
    /// </summary>
    public const string ApiVersion = "5.0";

    /// <summary>
    /// Descriptors are long strings and go on the query string, so identity
    /// reads are chunked.
    /// </summary>
    public const int DescriptorBatchSize = 25;

    private readonly Func<string, Task<string?>> _getJsonAsync;
    private readonly Func<string, string, Task<string?>> _postJsonAsync;

    /// <param name="getJsonAsync">
    /// Issues a GET against a request url relative to the collection, returning
    /// the response body, or null when the call failed.
    /// </param>
    /// <param name="postJsonAsync">
    /// Issues a POST with a JSON body against a request url relative to the
    /// collection, returning the response body, or null when the call failed.
    /// </param>
    public SecurityApiClient(
        Func<string, Task<string?>> getJsonAsync,
        Func<string, string, Task<string?>> postJsonAsync)
    {
        _getJsonAsync = getJsonAsync ?? throw new ArgumentNullException(nameof(getJsonAsync));
        _postJsonAsync = postJsonAsync ?? throw new ArgumentNullException(nameof(postJsonAsync));
    }

    public async Task<IReadOnlyList<SecurityNamespaceInfo>> GetSecurityNamespacesAsync()
    {
        var requestUrl = $"_apis/securitynamespaces?api-version={ApiVersion}";

        var json = await _getJsonAsync(requestUrl);

        var results = new List<SecurityNamespaceInfo>();

        if (string.IsNullOrWhiteSpace(json) == true)
        {
            return results;
        }

        using var document = JsonDocument.Parse(json);

        foreach (var item in EnumerateValueArray(document))
        {
            var info = new SecurityNamespaceInfo
            {
                NamespaceId = ReadString(item, "namespaceId"),
                Name = ReadString(item, "name"),
                DisplayName = ReadString(item, "displayName")
            };

            if (item.TryGetProperty("actions", out var actions) &&
                actions.ValueKind == JsonValueKind.Array)
            {
                foreach (var action in actions.EnumerateArray())
                {
                    info.Actions.Add(new SecurityNamespaceAction
                    {
                        Bit = ReadInt(action, "bit"),
                        Name = ReadString(action, "name"),
                        DisplayName = ReadString(action, "displayName")
                    });
                }
            }

            results.Add(info);
        }

        return results;
    }

    public async Task<IReadOnlyList<AccessControlListInfo>> GetAccessControlListsAsync(
        string namespaceId)
    {
        var requestUrl =
            $"_apis/accesscontrollists/{namespaceId}?api-version={ApiVersion}";

        var json = await _getJsonAsync(requestUrl);

        var results = new List<AccessControlListInfo>();

        if (string.IsNullOrWhiteSpace(json) == true)
        {
            return results;
        }

        using var document = JsonDocument.Parse(json);

        foreach (var item in EnumerateValueArray(document))
        {
            var acl = new AccessControlListInfo
            {
                Token = ReadString(item, "token"),
                InheritPermissions = ReadBool(item, "inheritPermissions", true)
            };

            if (item.TryGetProperty("acesDictionary", out var aces) &&
                aces.ValueKind == JsonValueKind.Object)
            {
                foreach (var pair in aces.EnumerateObject())
                {
                    acl.Entries.Add(new AccessControlEntryInfo
                    {
                        Descriptor = ReadString(pair.Value, "descriptor", pair.Name),
                        Allow = ReadInt(pair.Value, "allow"),
                        Deny = ReadInt(pair.Value, "deny")
                    });
                }
            }

            results.Add(acl);
        }

        return results;
    }

    public async Task<IReadOnlyList<TfsIdentityInfo>> ReadIdentitiesByDescriptorsAsync(
        IReadOnlyList<string> descriptors, bool includeDirectMembership)
    {
        var results = new List<TfsIdentityInfo>();

        foreach (var chunk in descriptors.Chunk(DescriptorBatchSize))
        {
            // Descriptors contain ';' and '\', so each is escaped individually;
            // the commas separating them stay literal.
            var descriptorList = string.Join(
                ",", chunk.Select(Uri.EscapeDataString));

            var queryMembership = includeDirectMembership == true ?
                "&queryMembership=Direct" : string.Empty;

            var requestUrl =
                $"_apis/identities?descriptors={descriptorList}{queryMembership}" +
                $"&api-version={ApiVersion}";

            var json = await _getJsonAsync(requestUrl);

            results.AddRange(ParseIdentities(json));
        }

        return results;
    }

    public async Task<TfsIdentityInfo?> ReadIdentityByAccountNameAsync(string accountName)
    {
        var requestUrl =
            $"_apis/identities?searchFilter=AccountName" +
            $"&filterValue={Uri.EscapeDataString(accountName)}" +
            $"&api-version={ApiVersion}";

        var json = await _getJsonAsync(requestUrl);

        var identities = ParseIdentities(json);

        if (identities.Count == 0)
        {
            return null;
        }

        return identities.FirstOrDefault(x =>
                string.Equals(x.AccountName, accountName, StringComparison.OrdinalIgnoreCase))
            ?? identities[0];
    }

    public async Task<bool> SetAccessControlEntriesAsync(
        string namespaceId, string token, IReadOnlyList<AccessControlEntryInfo> entries)
    {
        var requestUrl =
            $"_apis/accesscontrolentries/{namespaceId}?api-version={ApiVersion}";

        var body = new
        {
            token,
            merge = true,
            accessControlEntries = entries.Select(entry => new
            {
                descriptor = entry.Descriptor,
                allow = entry.Allow,
                deny = entry.Deny
            }).ToArray()
        };

        var response = await _postJsonAsync(requestUrl, JsonSerializer.Serialize(body));

        return response != null;
    }

    private static List<TfsIdentityInfo> ParseIdentities(string? json)
    {
        var results = new List<TfsIdentityInfo>();

        if (string.IsNullOrWhiteSpace(json) == true)
        {
            return results;
        }

        using var document = JsonDocument.Parse(json);

        foreach (var item in EnumerateValueArray(document))
        {
            var identity = new TfsIdentityInfo
            {
                Id = ReadString(item, "id"),
                Descriptor = ReadString(item, "descriptor"),
                ProviderDisplayName = ReadString(item, "providerDisplayName"),
                CustomDisplayName = ReadString(item, "customDisplayName"),
                IsContainer = ReadBool(item, "isContainer", false)
            };

            if (item.TryGetProperty("members", out var members) &&
                members.ValueKind == JsonValueKind.Array)
            {
                foreach (var member in members.EnumerateArray())
                {
                    if (member.ValueKind == JsonValueKind.String)
                    {
                        var value = member.GetString();

                        if (string.IsNullOrWhiteSpace(value) == false)
                        {
                            identity.MemberDescriptors.Add(value);
                        }
                    }
                }
            }

            identity.Account = ReadPropertyBagValue(item, "Account");
            identity.Domain = ReadPropertyBagValue(item, "Domain");
            identity.SchemaClassName = ReadPropertyBagValue(item, "SchemaClassName");

            results.Add(identity);
        }

        return results;
    }

    /// <summary>
    /// Identity properties arrive wrapped as {"$type":"System.String","$value":"..."}.
    /// </summary>
    private static string ReadPropertyBagValue(JsonElement identity, string key)
    {
        if (identity.TryGetProperty("properties", out var properties) == false ||
            properties.ValueKind != JsonValueKind.Object ||
            properties.TryGetProperty(key, out var wrapper) == false)
        {
            return string.Empty;
        }

        if (wrapper.ValueKind == JsonValueKind.String)
        {
            return wrapper.GetString() ?? string.Empty;
        }

        if (wrapper.ValueKind == JsonValueKind.Object &&
            wrapper.TryGetProperty("$value", out var value) &&
            value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static IEnumerable<JsonElement> EnumerateValueArray(JsonDocument document)
    {
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            document.RootElement.TryGetProperty("value", out var value) == false ||
            value.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object)
            {
                yield return item;
            }
        }
    }

    private static string ReadString(JsonElement element, string propertyName, string defaultValue = "")
    {
        if (element.TryGetProperty(propertyName, out var prop) == false)
        {
            return defaultValue;
        }

        return prop.ValueKind == JsonValueKind.String ? (prop.GetString() ?? defaultValue) : defaultValue;
    }

    private static int ReadInt(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) == false)
        {
            return 0;
        }

        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var value))
        {
            return value;
        }

        return 0;
    }

    private static bool ReadBool(JsonElement element, string propertyName, bool defaultValue)
    {
        if (element.TryGetProperty(propertyName, out var prop) == false)
        {
            return defaultValue;
        }

        return prop.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => defaultValue
        };
    }
}
