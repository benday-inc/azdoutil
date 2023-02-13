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

    private DateTime _startDate = new();
    private DateTime _endDate = new();
    private string _iterationName = string.Empty;
    private string _teamProjectName = string.Empty;

    protected override ArgumentCollection GetAvailableArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);
        arguments.AddString(Constants.CommandArg_TeamProjectName).WithDescription("Team project name");
        arguments.AddDateTime(Constants.CommandArg_StartDate).WithDescription("Iteration start date");
        arguments.AddDateTime(Constants.CommandArg_EndDate).WithDescription("Iteration end date");
        arguments.AddString(Constants.CommandArg_IterationName).WithDescription("Iteration name");

        return arguments;
    }


    protected override async Task OnExecute()
    {
        _startDate = Arguments.GetDateTimeValue(Constants.CommandArg_StartDate);
        _endDate = Arguments.GetDateTimeValue(Constants.CommandArg_EndDate);
        _iterationName = Arguments.GetStringValue(Constants.CommandArg_IterationName);
        _teamProjectName = Arguments.GetStringValue(Constants.CommandArg_TeamProjectName);

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

    private async Task<ClassificationNodeChild?> GetIterationByName(string iterationName)
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

}
