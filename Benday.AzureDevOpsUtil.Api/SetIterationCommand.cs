using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
namespace Benday.AzureDevOpsUtil.Api;

[Command(Name = Constants.CommandName_SetIteration,
        Description = "Create iteration including start and end date",
        IsAsync = true)]
public class SetIterationCommand : AzureDevOpsCommandBase
{
    public SetIterationCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override async Task Run()
    {
        Initialize();

        var iteration = await GetIterationByName(_iterationName);

        if (iteration == null)
        {
            await CreateNewIteration();
        }
        else
        {
            await UpdateIteration(iteration);
        }
    }
    private async Task CreateNewIteration()
    {
        CreateIterationRequest iteration = new();
        iteration.Attributes = new();

        iteration.Name = _iterationName;
        iteration.Attributes.StartDate = _startDate;
        iteration.Attributes.FinishDate = _endDate;

        var requestUrl = $"{System.Web.HttpUtility.HtmlEncode(_teamProjectName)}/_apis/wit/classificationnodes/Iterations?api-version=5.0&$depth=5";

        await SendPostForBodyAndGetTypedResponseSingleAttempt<ClassificationNodeChild, CreateIterationRequest>(
            requestUrl, iteration);

        Console.WriteLine($"done");
    }

    private async Task UpdateIteration(ClassificationNodeChild iteration)
    {
        if (iteration.Attributes == null)
        {
            iteration.Attributes = new();
        }

        iteration.Attributes.StartDate = _startDate;
        iteration.Attributes.FinishDate = _endDate;

        var requestUrl = $"{_teamProjectName}/_apis/wit/classificationnodes/Iterations?api-version=5.0&$depth=5";

        var result = await SendPostForBodyAndGetTypedResponseSingleAttempt<ClassificationNodeChild, ClassificationNodeChild>(
            requestUrl, iteration);

        Console.WriteLine($"done");
    }

    protected override string CommandArgumentName => UtilityConstants.CommandName_SetIteration;

    protected override List<string> GetRequiredArguments()
    {
        var temp = new List<string>
            {
                UtilityConstants.CommandArg_TeamProjectName,
                UtilityConstants.CommandArg_StartDate,
                UtilityConstants.CommandArg_EndDate,
                UtilityConstants.CommandArg_IterationName
            };

        return temp;
    }

    private DateTime _startDate = new();
    private DateTime _endDate = new();
    private string _iterationName = string.Empty;

    protected override void AfterValidateArguments()
    {
        _startDate = AssertArgValueExistsDateTime(UtilityConstants.CommandArg_StartDate);
        _endDate = AssertArgValueExistsDateTime(UtilityConstants.CommandArg_EndDate);
        _iterationName = AssertArgValueExists(UtilityConstants.CommandArg_IterationName);
    }

    private async Task<ClassificationNodeChild> GetIterationByName(string iterationName)
    {
        var requestUrl = $"{_teamProjectName}/_apis/wit/classificationnodes?api-version=5.0&$depth=5";

        var result = await CallEndpointViaGetAndGetResult<GetClassificationNodeResponse>(requestUrl, false);

        if (result != null)
        {
            foreach (var item in result.Value)
            {
                if (item.StructureType == "iteration")
                {
                    // find the root level iteration node

                    if (item.Children == null)
                    {
                        break;
                    }

                    foreach (var child in item.Children)
                    {
                        if (child.StructureType != "iteration")
                        {
                            continue;
                        }
                        else if (string.Equals(child.Name, iterationName, StringComparison.CurrentCultureIgnoreCase) == false)
                        {
                            continue;
                        }
                        else
                        {
                            return child;
                        }
                    }
                }
            }
        }

        return null;
    }

    protected async Task<TResponse> SendPostForBodyAndGetTypedResponseSingleAttempt<TResponse, TRequest>(
            string requestUrl,
            TRequest body, bool writeStringContentToInfo = false,
            string optionalDebuggingMessageInfo = null
            )
    {
        if (string.IsNullOrEmpty(requestUrl))
        {
            throw new ArgumentException($"{nameof(requestUrl)} is null or empty.", nameof(requestUrl));
        }

        using var client = GetHttpClientInstance(_tpcBaseUrl, _tokenAsBase64);

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
            Logger.LogInfo(responseContent);
        }

        var typedResponse = GetJsonValueAsType<TResponse>(responseContent);

        return typedResponse;
    }
}
