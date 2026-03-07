using System;
using System.Collections.Generic;
using System.Text;

using Benday.AzureDevOpsUtil.Api.ScriptGenerator;
using Benday.Common;

namespace Benday.AzureDevOpsUtil.Api.Commands.ProcessTemplates;

public class ProcessTemplateCreationInfo
{
    public bool UseRefinement { get; set; } = false;
    public string TemplateName { get; set; } = string.Empty;
    public string RequirementWorkItemTypeFullName { get; set; } = string.Empty;
    public string RequirementWorkItemTypeAbbreviationName { get; set; } = string.Empty;

    public string DoneStateName { get; set; } = string.Empty;
    public string InProgressStateName { get; set; } = string.Empty;
    public string ReadyForSprintStateName { get; set; } = string.Empty;
    public string RefinementStateName { get; set; } = string.Empty;
    public string AcceptedOnSprintBacklogStateName { get; set; } = string.Empty;

    public string GetWorkItemTypeForAction(WorkItemScriptAction action)
    {
        if (action.Definition.WorkItemType.EqualsCaseInsensitive("PBI") ||
            action.Definition.WorkItemType.EqualsCaseInsensitive("Product Backlog Item") ||
            action.Definition.WorkItemType.EqualsCaseInsensitive("User Story"))
        {
            if (RequirementWorkItemTypeFullName.IsNullOrEmpty())
            {
                throw new InvalidOperationException($"{nameof(RequirementWorkItemTypeFullName)} cannot be null or empty");
            }

            return RequirementWorkItemTypeFullName;
        }
        else if (
            action.Definition.WorkItemType.EqualsCaseInsensitive("Task"))
        {
            return "Task";
        }
        else
        {
            throw new InvalidOperationException($"Unknown work item script action work item type: {action.Definition.WorkItemType}");
        }
    }
}