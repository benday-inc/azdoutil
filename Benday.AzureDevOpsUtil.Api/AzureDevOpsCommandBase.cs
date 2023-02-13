using Benday.CommandsFramework;
using System.Net.Http.Headers;

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
}
