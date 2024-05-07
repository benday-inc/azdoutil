using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Benday.AzureDevOpsUtil.Api;

public abstract class AzureDevOpsCommandBase : AsynchronousCommand
{
    private AzureDevOpsConfiguration? _AzureDevOpsConfiguration;

    public AzureDevOpsCommandBase(
            CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }



    protected void AddCommonArguments(ArgumentCollection arguments)
    {
        arguments.AddBoolean(Constants.ArgumentNameQuietMode)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Quiet mode");

        arguments.AddString(Constants.ArgumentNameConfigurationName)
            .AsNotRequired().WithDescription("Configuration name to use");
    }

    public bool IsQuietMode
    {
        get
        {
            if (Arguments.ContainsKey(Constants.ArgumentNameQuietMode) == true &&
                Arguments[Constants.ArgumentNameQuietMode].HasValue)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
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

    protected HttpClient GetHttpClientInstanceForAzureDevOps()
    {
        if (Configuration.IsWindowsAuth == true)
        {
            var client = new HttpClient(
                new HttpClientHandler() {  UseDefaultCredentials = true });

            var baseUri = new Uri(Configuration.CollectionUrl);

            client.BaseAddress = baseUri;

            return client;
        }
        else
        {
            var client = new HttpClient();

            var baseUri = new Uri(Configuration.CollectionUrl);

            client.BaseAddress = baseUri;

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic",
                Configuration.GetTokenBase64Encoded());

            return client;
        }        
    }

    protected async Task<T?> CallEndpointViaGetAndGetResult<T>(string requestUrl, bool writeStringContentToInfo = false, bool throwExceptionOnError = true)
    {
        try
        {
            return await CallEndpointViaGetAndGetResultSingleAttempt<T>(requestUrl, writeStringContentToInfo, throwExceptionOnError);
        }
        catch
        {
            await Task.Delay(Constants.RetryDelayInMillisecs);

            var result = await CallEndpointViaGetAndGetResultSingleAttempt<T>(requestUrl, writeStringContentToInfo);

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

    protected async Task<T> SendPutForBodyAndGetTypedResponseSingleAttempt<T>(
        string requestUrl,
        T body, bool writeStringContentToInfo = false,
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

        var result = await client.PutAsync(requestUrl, 
            new StringContent(requestAsJson, Encoding.UTF8, "application/json"));

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
