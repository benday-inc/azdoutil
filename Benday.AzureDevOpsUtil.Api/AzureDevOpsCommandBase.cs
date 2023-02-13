using Benday.CommandsFramework;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace Benday.AzureDevOpsUtil.Api;

public abstract class AzureDevOpsCommandBase : AsynchronousCommand
{
    private AzureDevOpsConfiguration? _AzureDevOpsConfiguration;

    public AzureDevOpsCommandBase(
            CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    protected bool ArgumentBooleanValue(string argumentName)
    {
        if (Arguments.ContainsKey(argumentName) == false)
        {
            return false;
        }
        else
        {
            var argAsBool = Arguments[argumentName] as BooleanArgument;

            if (argAsBool == null)
            {
                throw new InvalidOperationException($"Cannot call ArgumentBooleanValue() on non-boolean arg '{argumentName}'.");
            }
            else
            {
                if (argAsBool.HasValue == false)
                {
                    return false;
                }
                else
                {
                    return argAsBool.ValueAsBoolean;
                }
            }
        }
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
                    throw new InvalidOperationException($"Could not find a configuration named '{configName}'.");
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
        var client = new HttpClient();

        var baseUri = new Uri(Configuration.CollectionUrl);

        client.BaseAddress = baseUri;

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", 
            Configuration.GetTokenBase64Encoded());

        return client;
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
}
