using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

using Benday.AzureDevOpsUtil.Api.Commands.ProjectAdministration;
namespace Benday.AzureDevOpsUtil.Api.Commands.ProcessTemplates;

[Command(
    Category = Constants.Category_ProcessTemplates,
    Name = Constants.CommandArgumentNameCreateBacklogRefinementProcessTemplate,
        Description = "Creates backlog refinement process template as described at https://www.benday.com/2022/09/29/streamlining-backlog-refinement-with-azure-devops/",
        IsAsync = true)]
public class CreateBacklogRefinementProcessTemplateCommand : AzureDevOpsCommandBase
{
    public CreateBacklogRefinementProcessTemplateCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    private ListProcessTemplatesResponse? ProcessTemplates { get; set; }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);

        arguments
            .AddBoolean("agile")
            .AllowEmptyValue()
            .AsNotRequired()
            .WithDescription("Whether to create an agile backlog refinement process template instead of scrum.");

        return arguments;
    }

    protected override async Task OnExecute()
    {
        var isAgile = Arguments.GetBooleanValue("agile");

        var execInfo = ExecutionInfo.GetCloneOfArguments(
                Constants.CommandName_ListProcessTemplates, true);

        execInfo.RemoveAllArgumentsExcept(true);

        var listProcessTemplates = new ListProcessTemplatesCommand(
             execInfo,
             _OutputProvider);

        await listProcessTemplates.ExecuteAsync();

        ProcessTemplates = listProcessTemplates.LastResult;

        if (ProcessTemplates == null || ProcessTemplates.Count == 0)
        {
            throw new KnownException("No process templates available on the server.");
        }
        else
        {
            var templateNameToCheck = isAgile ? Constants.ProcessTemplateName_AgileWithBacklogRefinement : Constants.ProcessTemplateName_ScrumWithBacklogRefinement;

            var match = ProcessTemplates.Values.Where(x =>
                string.Equals(x.Name,
                templateNameToCheck,
                StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

            if (match != null)
            {
                throw new KnownException(
                    $"Process template {templateNameToCheck} already exists.");
            }
            else
            {
                await CreateProcessTemplate(isAgile);
            }
        }
    }

    private async Task<CreateInheritedWorkItemTypeResponse?> CreateInheritedWorkItemType(string inheritedProcessId, bool isAgile)
    {
        var workItemNameToUse = isAgile ? "User Story" : "Product Backlog Item";
        var workItemRefNameToUse = isAgile ? "Microsoft.VSTS.WorkItemTypes.UserStory" : "Microsoft.VSTS.WorkItemTypes.ProductBacklogItem";

        var requestUrlCreateNewWorkItemType = $"_apis/work/processes/{inheritedProcessId}/workitemtypes?api-version=7.0";

        var newWorkItemRequest = new CreateInheritedWorkItemTypeRequest()
        {
            Name = workItemNameToUse,
            InheritsFromWorkItemRefName = workItemRefNameToUse,
            Description = "Tracks an activity the user will be able to perform with the product."
        };

        var newInheritedWorkItem = await SendPostForBodyAndGetTypedResponseSingleAttempt<CreateInheritedWorkItemTypeResponse, CreateInheritedWorkItemTypeRequest>(
            requestUrlCreateNewWorkItemType, newWorkItemRequest);

        return newInheritedWorkItem;
    }

    private async Task CreateProcessTemplate(bool isAgile)
    {
        var parentProcessName = isAgile ? Constants.ProcessTemplateName_Agile : Constants.ProcessTemplateName_Scrum;

        var match = ProcessTemplates!.Values.Where(x =>
                string.Equals(x.Name,
                parentProcessName,
                StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

        if (match == null)
        {
            throw new KnownException(
                    $"Error: Could not locate parent process templates {parentProcessName}.");
        }

        // create the work item process
        var newInheritedProcess = await CreateInheritedProcessTemplate(match, isAgile);

        if (newInheritedProcess == null)
        {
            throw new KnownException(
                    $"Error: call to create inherited process template returned no results.");
        }

        // create the inherited work item

        string newInheritedProcessId = newInheritedProcess.Id;

        var newInheritedWorkItem = await CreateInheritedWorkItemType(newInheritedProcessId, isAgile);

        if (newInheritedWorkItem == null)
        {
            throw new KnownException(
                    $"Error: call to create inherited work item type returned no results.");
        }

        // create the work item states
        string newInheritedWorkItemRefName = newInheritedWorkItem.RefName;

        await CreateNewWorkItemState(newInheritedProcessId, newInheritedWorkItemRefName, "Needs Refinement");
        await CreateNewWorkItemState(newInheritedProcessId, newInheritedWorkItemRefName, "Ready for Sprint");

        WriteLine("Done.");
    }

    private async Task<CreateWorkItemStateResponse?> CreateNewWorkItemState(string newInheritedProcessId,
        string newInheritedWorkItemRefName, string newState)
    {
        var requestUrlCreateNewWorkItemState = $"_apis/work/processes/{newInheritedProcessId}/workitemtypes/{newInheritedWorkItemRefName}/states?api-version=7.0";

        var newWorkItemStateRequest = new CreateWorkItemStateRequest()
        {
            Name = newState,
            StateCategory = "Proposed"
        };

        var newInheritedWorkItemState = await SendPostForBodyAndGetTypedResponseSingleAttempt<CreateWorkItemStateResponse, CreateWorkItemStateRequest>(
            requestUrlCreateNewWorkItemState, newWorkItemStateRequest);

        if (newInheritedWorkItemState == null)
        {
            throw new KnownException(
                    $"Error: call to create inherited work item state returned no results.");
        }

        return newInheritedWorkItemState;
    }

    private async Task<ProcessTemplateDetailInfo> CreateInheritedProcessTemplate(ProcessTemplateDetailInfo match, bool isAgile)
    {
        var templateNameToUse = isAgile ? Constants.ProcessTemplateName_AgileWithBacklogRefinement : Constants.ProcessTemplateName_ScrumWithBacklogRefinement;
        var referenceNameToUse = isAgile ? Constants.ProcessTemplateRefName_AgileWithBacklogRefinement : Constants.ProcessTemplateRefName_ScrumWithBacklogRefinement;

        var requestUrlCreateNewProcess = $"_apis/work/processes?api-version=7.0";

        var newProcessRequest = new CreateInheritedProcessRequest()
        {
            Name = templateNameToUse,
            ParentProcessTypeId = match.Id,
            ReferenceName = referenceNameToUse,
            Description = match.Description
        };

        var newInheritedProcess = await SendPostForBodyAndGetTypedResponseSingleAttempt<ProcessTemplateDetailInfo, CreateInheritedProcessRequest>(
            requestUrlCreateNewProcess, newProcessRequest);
        return newInheritedProcess;
    }
}