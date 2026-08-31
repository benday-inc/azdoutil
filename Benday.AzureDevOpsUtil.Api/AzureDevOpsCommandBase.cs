using Benday.AzureDevOpsUtil.Api.ApiVersioning;
using Benday.AzureDevOpsUtil.Api.GitRemotes;
using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Benday.AzureDevOpsUtil.Api;

public abstract class AzureDevOpsCommandBase : Command
{
    private AzureDevOpsConfiguration? _AzureDevOpsConfiguration;

    public AzureDevOpsCommandBase(
            CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }



    protected void AddCommonArguments(ArgumentCollection arguments)
    {
        // 'quiet' is a name the framework reserves, so declaring it here only adds the
        // description to this command's usage output -- the value itself is read by
        // CommandBase.IsQuietMode straight off the parsed command line
        arguments
            .AddBoolean(Constants.ArgumentNameQuietMode)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Quiet mode");

        arguments.AddString(Constants.ArgumentNameConfigurationName)
            .AsNotRequired().WithDescription("Configuration name to use");
    }

    protected AzureDevOpsConfiguration Configuration
    {
        get
        {
            if (_AzureDevOpsConfiguration == null)
            {
                var configName = GetConfigurationName();

                var temp =
                    AzureDevOpsConfigurationManager.Instance.Get(configName);

                if (temp == null)
                {
                    throw new KnownException($"Could not find a configuration named '{configName}'. Add a configuration and try again.");
                }

                _AzureDevOpsConfiguration = temp;
            }

            return _AzureDevOpsConfiguration;
        }

        set => _AzureDevOpsConfiguration = value;
    }

    /// <summary>
    /// The value of an argument that may not have been supplied, or an empty
    /// string.
    /// </summary>
    protected string GetOptionalStringValue(string argumentName)
    {
        if (Arguments.ContainsKey(argumentName) == true &&
            Arguments[argumentName].HasValue == true)
        {
            return Arguments.GetStringValue(argumentName) ?? string.Empty;
        }

        return string.Empty;
    }

    /// <summary>
    /// Works out which Azure DevOps repository the current directory belongs
    /// to, for commands that can take their arguments from the git remote
    /// rather than the command line.
    ///
    /// Each way this can fail says something different, so each gets its own
    /// message rather than a generic missing-argument error.
    /// </summary>
    protected GitRemoteInfo ReadCurrentRepositoryRemote()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        var gitDirectory = GitRepositoryLocator.FindGitDirectory(currentDirectory);

        if (gitDirectory == null)
        {
            throw new KnownException(
                $"'{currentDirectory}' is not inside a git repository, so there is nothing to " +
                $"read the repository name from. Supply --{Constants.ArgumentNameTeamProjectName} " +
                $"and --{Constants.ArgumentNameRepositoryName}.");
        }

        var remoteUrl = GitRepositoryLocator.FindRemoteUrl(currentDirectory);

        if (string.IsNullOrWhiteSpace(remoteUrl) == true)
        {
            throw new KnownException(
                "This git repository has no 'origin' remote to read the repository name from. " +
                $"Supply --{Constants.ArgumentNameTeamProjectName} and " +
                $"--{Constants.ArgumentNameRepositoryName}.");
        }

        var remote = GitRemoteUrlParser.Parse(remoteUrl);

        if (remote == null)
        {
            throw new KnownException(
                $"The origin remote of this git repository is '{remoteUrl}', which is not an " +
                $"Azure DevOps repository url. Supply --{Constants.ArgumentNameTeamProjectName} " +
                $"and --{Constants.ArgumentNameRepositoryName}.");
        }

        return remote;
    }

    /// <summary>
    /// Picks the stored configuration that talks to the collection the remote
    /// points at.  Detecting a url does not supply credentials, so this is what
    /// makes the detected repository reachable.  An explicit /config wins.
    /// </summary>
    protected void UseConfigurationForRemote(GitRemoteInfo remote)
    {
        if (Arguments.ContainsKey(Constants.ArgumentNameConfigurationName) == true &&
            Arguments[Constants.ArgumentNameConfigurationName].HasValue == true)
        {
            return;
        }

        var configurations = AzureDevOpsConfigurationManager.Instance.GetAll();

        var match = configurations.FirstOrDefault(x =>
            AreSameCollection(x.CollectionUrl, remote.CollectionUrl));

        if (match == null)
        {
            var known = configurations.Length == 0 ?
                "There are no configurations." :
                "Configurations: " + string.Join(
                    ", ", configurations.Select(x => $"{x.Name} ({x.CollectionUrl})")) + ".";

            throw new KnownException(
                $"The origin remote points at {remote.CollectionUrl}, and no azdoutil " +
                $"configuration uses that url. {known} Add one with " +
                $"{Constants.CommandArgumentNameAddUpdateConfig}, or name a configuration with " +
                $"--{Constants.ArgumentNameConfigurationName}.");
        }

        Configuration = match;
    }

    private static bool AreSameCollection(string? left, string? right)
    {
        var trimmedLeft = (left ?? string.Empty).TrimEnd('/');
        var trimmedRight = (right ?? string.Empty).TrimEnd('/');

        return string.Equals(trimmedLeft, trimmedRight, StringComparison.OrdinalIgnoreCase);
    }

    protected string GetConfigurationName()
    {
        if (Arguments.ContainsKey(Constants.ArgumentNameConfigurationName) == true &&
            Arguments[Constants.ArgumentNameConfigurationName].HasValue)
        {
            var configName = Arguments[Constants.ArgumentNameConfigurationName].Value;

            return configName;
        }
        else
        {
            return Constants.DefaultConfigurationName;
        }
    }

    /// <summary>
    /// Wires an <see cref="AgentCapabilities.AgentCapabilityService"/> to this
    /// command's authenticated GET and PUT, so the capability commands share one
    /// place that knows how to reach the agent pool endpoints.
    /// </summary>
    protected AgentCapabilities.AgentCapabilityService CreateAgentCapabilityService()
    {
        var client = new AgentCapabilities.AgentPoolClient(
            url => GetStringAsync(url, false, true),
            (url, body) => SendPutForBodySingleAttempt(url, body, true));

        return new AgentCapabilities.AgentCapabilityService(client);
    }

    protected HttpClient GetHttpClientInstanceForAzureDevOps(
        AzureDevOpsUrlTargetType azureDevOpsUrlTargetType = AzureDevOpsUrlTargetType.Default)
    {
        return CreateHttpClient(azureDevOpsUrlTargetType, clampApiVersion: true);
    }

    /// <summary>
    /// The authenticated client, optionally without the api-version clamp.
    ///
    /// The clamp is off for the OPTIONS probe that feeds the clamp, which would
    /// otherwise be asking itself what it is allowed to ask.
    /// </summary>
    private HttpClient CreateHttpClient(
        AzureDevOpsUrlTargetType azureDevOpsUrlTargetType, bool clampApiVersion)
    {
        var baseUrl = Configuration.CollectionUrl;

        if (azureDevOpsUrlTargetType == AzureDevOpsUrlTargetType.Release &&
            Configuration.IsAzureDevOpsService == true)
        {
            baseUrl = baseUrl.Replace("https://dev.", "https://vsrm.dev.");
        }

        var baseUri = new Uri(baseUrl);

        HttpMessageHandler handler = Configuration.IsWindowsAuth == true ?
            new HttpClientHandler() { UseDefaultCredentials = true } :
            new HttpClientHandler();

        if (clampApiVersion == true)
        {
            ApplyPinnedApiVersion();

            handler = new ApiVersionClampingHandler(
                handler, Configuration.CollectionUrl, ProbeApiVersionCatalog);
        }

        var client = new HttpClient(handler)
        {
            BaseAddress = baseUri
        };

        if (Configuration.IsWindowsAuth == false)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic",
                Configuration.GetTokenBase64Encoded());
        }

        return client;
    }

    /// <summary>
    /// Asks the collection what it can serve.  OPTIONS on the _apis root takes
    /// no api-version of its own, which is what makes it answerable by a server
    /// of any age.
    /// </summary>
    private async Task<string?> ProbeApiVersionCatalog(CancellationToken cancellationToken)
    {
        using var client = CreateHttpClient(
            AzureDevOpsUrlTargetType.Default, clampApiVersion: false);

        using var request = new HttpRequestMessage(HttpMethod.Options, "_apis");

        using var response = await client.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode == false)
        {
            return null;
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// The server's own build, read off its About page.
    ///
    /// This is a web page rather than an endpoint, because there is no endpoint
    /// -- see <see cref="ServerVersionReader"/>.  A server that will not serve
    /// it, or serves a sign-in page instead, produces an empty result rather
    /// than an error: this is diagnostic information, not something a command
    /// depends on.
    /// </summary>
    protected async Task<ServerVersionInfo> GetServerVersion(CancellationToken cancellationToken)
    {
        try
        {
            // no api-version on this url, so the clamp has nothing to do and is
            // left out to keep an html 404 out of its investigation path
            using var client = CreateHttpClient(
                AzureDevOpsUrlTargetType.Default, clampApiVersion: false);

            using var response = await client.GetAsync(
                ServerVersionReader.AboutPagePath, cancellationToken);

            if (response.IsSuccessStatusCode == false)
            {
                return new ServerVersionInfo();
            }

            return ServerVersionReader.Read(
                await response.Content.ReadAsStringAsync(cancellationToken));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ServerVersionInfo();
        }
    }

    /// <summary>
    /// Seeds the version cache from the stored configuration, so that a pinned
    /// version is in place before the first request rather than after the first
    /// failure.  Discovery is skipped entirely for a collection that has one.
    /// </summary>
    private void ApplyPinnedApiVersion()
    {
        if (string.IsNullOrWhiteSpace(Configuration.MaxApiVersion) == true ||
            ServerApiVersionCache.Get(Configuration.CollectionUrl) != null)
        {
            return;
        }

        if (ApiVersion.TryParse(Configuration.MaxApiVersion, out var pinned) == false)
        {
            throw new KnownException(
                $"Configuration '{Configuration.Name}' has a max api-version of " +
                $"'{Configuration.MaxApiVersion}', which is not an api-version. Expected " +
                $"something like 5.0. Fix it with {Constants.CommandArgumentNameAddUpdateConfig}.");
        }

        ServerApiVersionCache.Set(
            Configuration.CollectionUrl, ServerApiVersionInfo.Pinned(pinned));
    }

    /// <summary>
    /// What this collection says it can serve, probing it if that is not
    /// already known.  Returns null when the collection would not answer.
    /// </summary>
    protected async Task<ServerApiVersionInfo?> GetServerApiVersionInfo(
        CancellationToken cancellationToken)
    {
        ApplyPinnedApiVersion();

        var known = ServerApiVersionCache.Get(Configuration.CollectionUrl);

        if (known?.Catalog != null)
        {
            return known;
        }

        var catalog = ApiVersionCatalog.Parse(await ProbeApiVersionCatalog(cancellationToken));

        if (catalog == null)
        {
            return known;
        }

        var info = ServerApiVersionInfo.FromCatalog(catalog);

        // a pinned version was set deliberately and outranks what the collection
        // says about itself, so reporting must not overwrite it
        if (known?.IsPinned != true)
        {
            ServerApiVersionCache.Set(Configuration.CollectionUrl, info);
        }

        return info;
    }

    protected async Task<T?> CallEndpointViaGetAndGetResult<T>(
        string requestUrl, bool writeStringContentToInfo = false, bool throwExceptionOnError = true,
        AzureDevOpsUrlTargetType azureDevOpsUrlTargetType = AzureDevOpsUrlTargetType.Default)
    {
        try
        {
            return await CallEndpointViaGetAndGetResultSingleAttempt<T>(
                requestUrl, writeStringContentToInfo, throwExceptionOnError, azureDevOpsUrlTargetType);
        }
        catch
        {
            await Task.Delay(Constants.RetryDelayInMillisecs);

            var result = await CallEndpointViaGetAndGetResultSingleAttempt<T>(
                requestUrl, writeStringContentToInfo, azureDevOpsUrlTargetType: azureDevOpsUrlTargetType);

            return result;
        }
    }

    protected async Task<string?> GetStringAsync(
        string requestUrl, bool writeStringContentToInfo = false, bool throwExceptionOnError = true)
    {
        using var client = GetHttpClientInstanceForAzureDevOps();

        var result = await client.GetAsync(requestUrl);

        if (result.IsSuccessStatusCode == false && throwExceptionOnError == true)
        {
            throw new InvalidOperationException($"Problem with server call to {requestUrl}. {result.StatusCode} {result.ReasonPhrase}");
        }
        else if (result.IsSuccessStatusCode == false && throwExceptionOnError == false)
        {
            return default;
        }
        else
        {
            var responseContent = await result.Content.ReadAsStringAsync();

            if (writeStringContentToInfo == true)
            {
                WriteLine(responseContent);
            }

            return responseContent;
        }
    }

    private async Task<T?> CallEndpointViaGetAndGetResultSingleAttempt<T>(
        string requestUrl, bool writeStringContentToInfo = false, 
        bool throwExceptionOnError = true,
        AzureDevOpsUrlTargetType azureDevOpsUrlTargetType = AzureDevOpsUrlTargetType.Default)
    {
        using var client = GetHttpClientInstanceForAzureDevOps(azureDevOpsUrlTargetType);

        var result = await client.GetAsync(requestUrl);

        if (result.IsSuccessStatusCode == false && throwExceptionOnError == true)
        {
            throw new InvalidOperationException($"Problem with server call to {requestUrl}. {result.StatusCode} {result.ReasonPhrase}");
        }
        else if (result.IsSuccessStatusCode == false && throwExceptionOnError == false)
        {
            return default;
        }
        else
        {
            var responseContent = await result.Content.ReadAsStringAsync();

            if (writeStringContentToInfo == true)
            {
                WriteLine(responseContent);
            }

            var typedResponse = JsonUtilities.GetJsonValueAsType<T>(responseContent);

            return typedResponse!;
        }
    }

    protected async Task<TResponse> SendPostForBodyAndGetTypedResponseSingleAttempt<TResponse, TRequest>(
            string requestUrl,
            TRequest body, bool writeStringContentToInfo = false,
            string? optionalDebuggingMessageInfo = null
            )
    {
        if (string.IsNullOrEmpty(requestUrl))
        {
            throw new ArgumentException($"{nameof(requestUrl)} is null or empty.", nameof(requestUrl));
        }

        using var client = GetHttpClientInstanceForAzureDevOps();

        string requestAsJson;

        requestAsJson = JsonSerializer.Serialize(body);

        var request = new HttpRequestMessage(new HttpMethod("POST"), requestUrl)
        {
            Content = new StringContent(requestAsJson, Encoding.UTF8, "application/json")
        };

        var result = await client.SendAsync(request);

        if (result.IsSuccessStatusCode == false)
        {
            var content = await result.Content.ReadAsStringAsync();

            if (optionalDebuggingMessageInfo == null)
            {
                throw new InvalidOperationException(
                    $"Problem with server call to {requestUrl}. {result.StatusCode} {result.ReasonPhrase} - {content}");
            }
            else
            {
                throw new InvalidOperationException(
                     $"Problem with server call to {requestUrl}. Debug info = '{optionalDebuggingMessageInfo}'.  {result.StatusCode} {result.ReasonPhrase} - {content}");

            }
        }

        var responseContent = await result.Content.ReadAsStringAsync();

        if (writeStringContentToInfo == true)
        {
            WriteLine(responseContent);
        }

        var typedResponse = JsonUtilities.GetJsonValueAsType<TResponse>(responseContent);

        return typedResponse!;
    }

    protected async Task<bool> SendPatchForBody(
            string requestUrl,
            WorkItemFieldOperationValueCollection body,
            bool throwExceptionOnError = true
            )
    {
        try
        {
            return await SendPatchForBodySingleAttempt(requestUrl, body, throwExceptionOnError);
        }
        catch (Exception ex)
        {
            WriteLine($"{nameof(SendPatchForBody)} failed for '{requestUrl}' with error '{ex}'...retrying...");

            await Task.Delay(Constants.RetryDelayInMillisecs);

            var result = await SendPatchForBodySingleAttempt(requestUrl, body, throwExceptionOnError);

            WriteLine($"{nameof(SendPatchForBody)} retry to '{requestUrl}' succeeded.");

            return result;
        }
    }

    private async Task<bool> SendPatchForBodySingleAttempt(
        string requestUrl,
        WorkItemFieldOperationValueCollection body,
        bool throwExceptionOnError = true
        )
    {
        if (string.IsNullOrEmpty(requestUrl))
        {
            throw new ArgumentException($"{nameof(requestUrl)} is null or empty.", nameof(requestUrl));
        }

        if ((body == null) || (body.Count == 0))
        {
            throw new ArgumentException($"{nameof(body)} is null or empty.", nameof(body));
        }

        using var client = GetHttpClientInstanceForAzureDevOps();

        string requestAsJson;


        requestAsJson = JsonSerializer.Serialize(body.Values);

        var request = new HttpRequestMessage(new HttpMethod("PATCH"), requestUrl)
        {
            Content = new StringContent(requestAsJson, Encoding.UTF8, "application/json-patch+json")
        };

        var result = await client.SendAsync(request);

        if (result.IsSuccessStatusCode == false)
        {
            var content = await result.Content.ReadAsStringAsync();

            var likelyDeadlockError = false;

            if (content != null && content.Contains("TF400037") == true)
            {
                likelyDeadlockError = true;
            }

            if (likelyDeadlockError == true)
            {
                throw new ServerCallGotDeadlockMessageException(
                    $"Probable deadlock exception. Problem with server call to {requestUrl}. {result.StatusCode} {result.ReasonPhrase} - {content}");
            }
            else if (throwExceptionOnError == true)
            {
                throw new InvalidOperationException(
                    $"Problem with server call to {requestUrl}. {result.StatusCode} {result.ReasonPhrase} - {content}");
            }
            else
            {
                return false;
            }
        }
        else
        {
            return true;
        }
    }

    protected async Task<bool> SendPutForBodySingleAttempt(
        string requestUrl,
        string bodyJson,
        bool throwExceptionOnError = true
    )
    {
        if (string.IsNullOrEmpty(requestUrl))
        {
            throw new ArgumentException($"{nameof(requestUrl)} is null or empty.", nameof(requestUrl));
        }

        if (string.IsNullOrEmpty(bodyJson) == true)
        {
            throw new ArgumentException($"{nameof(bodyJson)} is null or empty.", nameof(bodyJson));
        }

        var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

        using var client = GetHttpClientInstanceForAzureDevOps();

        var request = new HttpRequestMessage(new HttpMethod("PUT"), requestUrl)
        {
            Content = content
        };

        var result = await client.SendAsync(request);

        if (result.IsSuccessStatusCode == false)
        {
            var responseContent = await result.Content.ReadAsStringAsync();

            if (throwExceptionOnError == true)
            {
                throw new InvalidOperationException(
                    $"Problem with server call to {requestUrl}. {result.StatusCode} {result.ReasonPhrase} - {responseContent}");
            }
            else
            {
                return false;
            }
        }
        else
        {
            return true;
        }
    }

    protected async Task<bool> SendPutForBodySingleAttempt(
        HttpClient client,
        string requestUrl,
        string bodyJson,
        bool throwExceptionOnError = true
    )
    {
        if (string.IsNullOrEmpty(requestUrl))
        {
            throw new ArgumentException($"{nameof(requestUrl)} is null or empty.", nameof(requestUrl));
        }

        if (string.IsNullOrEmpty(bodyJson) == true)
        {
            throw new ArgumentException($"{nameof(bodyJson)} is null or empty.", nameof(bodyJson));
        }

        var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(new HttpMethod("PUT"), requestUrl)
        {
            Content = content
        };

        var result = await client.SendAsync(request);

        if (result.IsSuccessStatusCode == false)
        {
            var responseContent = await result.Content.ReadAsStringAsync();

            if (throwExceptionOnError == true)
            {
                throw new InvalidOperationException(
                    $"Problem with server call to {requestUrl}. {result.StatusCode} {result.ReasonPhrase} - {responseContent}");
            }
            else
            {
                return false;
            }
        }
        else
        {
            return true;
        }
    }

    protected async Task<T> SendPatchForBodyAndGetTypedResponse<T>(
        string requestUrl,
        WorkItemFieldOperationValueCollection body, bool writeStringContentToInfo = false,
        string? optionalDebuggingMessageInfo = null
        )
    {
        try
        {
            return await SendPatchForBodyAndGetTypedResponseSingleAttempt<T>(requestUrl, body, writeStringContentToInfo, optionalDebuggingMessageInfo);
        }
        catch (Exception ex)
        {
            WriteLine($"{nameof(SendPatchForBodyAndGetTypedResponse)} failed for '{requestUrl}' with error '{ex}'...retrying...");

            await Task.Delay(Constants.RetryDelayInMillisecs);

            var result = await SendPatchForBodyAndGetTypedResponseSingleAttempt<T>(requestUrl, body, writeStringContentToInfo, optionalDebuggingMessageInfo);

            WriteLine($"{nameof(SendPatchForBodyAndGetTypedResponse)} retry to '{requestUrl}' succeeded.");

            return result;
        }
    }

    protected async Task<T> SendPatchForBodyAndGetTypedResponseSingleAttempt<T>(
        string requestUrl,
        WorkItemFieldOperationValueCollection body, bool writeStringContentToInfo = false,
        string? optionalDebuggingMessageInfo = null
        )
    {
        if (string.IsNullOrEmpty(requestUrl))
        {
            throw new ArgumentException($"{nameof(requestUrl)} is null or empty.", nameof(requestUrl));
        }

        if ((body == null) || (body.Count == 0))
        {
            throw new ArgumentException($"{nameof(body)} is null or empty.", nameof(body));
        }

        using var client = GetHttpClientInstanceForAzureDevOps();

        string requestAsJson;

        requestAsJson = JsonSerializer.Serialize(body.Values);

        var request = new HttpRequestMessage(new HttpMethod("PATCH"), requestUrl)
        {
            Content = new StringContent(requestAsJson, Encoding.UTF8, "application/json-patch+json")
        };

        var result = await client.SendAsync(request);

        if (result.IsSuccessStatusCode == false)
        {
            var content = await result.Content.ReadAsStringAsync();

            if (optionalDebuggingMessageInfo == null)
            {
                throw new InvalidOperationException(
                        $"Problem with server call to {requestUrl}. {result.StatusCode} {result.ReasonPhrase} - {content}");
            }
            else
            {
                throw new InvalidOperationException(
                                $"Problem with server call to {requestUrl}. Debug info = '{optionalDebuggingMessageInfo}'.  {result.StatusCode} {result.ReasonPhrase} - {content}");

            }
        }

        var responseContent = await result.Content.ReadAsStringAsync();

        if (writeStringContentToInfo == true)
        {
            WriteLine(responseContent);
        }

        var typedResponse = JsonUtilities.GetJsonValueAsType<T>(responseContent);

        return typedResponse;
    }

    protected static void AssertFileExists(string path, string argumentName)
    {
        if (File.Exists(path) == false)
        {
            var message = string.Format(
                "File for argument '{0}' was not found.", argumentName);

            throw new FileNotFoundException(
                message, path);
        }
    }
}
