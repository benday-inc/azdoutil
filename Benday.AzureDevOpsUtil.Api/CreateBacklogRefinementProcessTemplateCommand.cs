using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

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

        return arguments;
    }

    protected override async Task OnExecute()
    {
        var listProcessTemplates = new ListProcessTemplatesCommand(
             ExecutionInfo.GetCloneOfArguments(
                Constants.CommandName_ListProcessTemplates, true),
             _OutputProvider);

        await listProcessTemplates.ExecuteAsync();

        ProcessTemplates = listProcessTemplates.LastResult;

        if (ProcessTemplates == null || ProcessTemplates.Count == 0)
        {
            throw new KnownException("No process templates available on the server.");
        }
        else
        {
            var match = ProcessTemplates.Values.Where(x => 
                string.Equals(x.Name, 
                Constants.ProcessTemplateName_ScrumWithBacklogRefinement, 
                StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

            if (match != null)
            {
                throw new KnownException(
                    $"Process templates {Constants.ProcessTemplateName_ScrumWithBacklogRefinement} already exists.");
            }
            else
            {
                await CreateProcessTemplate();
            }
        }
    }

    private async Task<CreateInheritedWorkItemTypeResponse?> CreateInheritedWorkItemType(string inheritedProcessId)
    {
        var requestUrlCreateNewWorkItemType = $"_apis/work/processes/{inheritedProcessId}/workitemtypes?api-version=7.0";

        var newWorkItemRequest = new CreateInheritedWorkItemTypeRequest()
        {
            Name = "Product Backlog Item",
            InheritsFromWorkItemRefName = "Microsoft.VSTS.WorkItemTypes.ProductBacklogItem",
            Description = "Tracks an activity the user will be able to perform with the product."
        };

        var newInheritedWorkItem = await SendPostForBodyAndGetTypedResponseSingleAttempt<CreateInheritedWorkItemTypeResponse, CreateInheritedWorkItemTypeRequest>(
            requestUrlCreateNewWorkItemType, newWorkItemRequest);

        return newInheritedWorkItem;
    }

    private async Task CreateProcessTemplate()
    {
        var match = ProcessTemplates!.Values.Where(x =>
                string.Equals(x.Name,
                Constants.ProcessTemplateName_Scrum,
                StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

        if (match == null)
        {
            throw new KnownException(
                    $"Error: Could not locate parent process templates {Constants.ProcessTemplateName_Scrum}.");
        }

        // create the work item process
        var newInheritedProcess = await CreateInheritedProcessTemplate(match);

        if (newInheritedProcess == null)
        {
            throw new KnownException(
                    $"Error: call to create inherited process template returned no results.");
        }

        // create the inherited work item

        string newInheritedProcessId = newInheritedProcess.Id;

        var newInheritedWorkItem = await CreateInheritedWorkItemType(newInheritedProcessId);

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

    private async Task<ProcessTemplateDetailInfo> CreateInheritedProcessTemplate(ProcessTemplateDetailInfo match)
    {
        var requestUrlCreateNewProcess = $"_apis/work/processes?api-version=7.0";

        var newProcessRequest = new CreateInheritedProcessRequest()
        {
            Name = Constants.ProcessTemplateName_ScrumWithBacklogRefinement,
            ParentProcessTypeId = match.Id,
            ReferenceName = Constants.ProcessTemplateRefName_ScrumWithBacklogRefinement,
            Description = match.Description
        };

        var newInheritedProcess = await SendPostForBodyAndGetTypedResponseSingleAttempt<ProcessTemplateDetailInfo, CreateInheritedProcessRequest>(
            requestUrlCreateNewProcess, newProcessRequest);
        return newInheritedProcess;
    }
}