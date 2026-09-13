namespace Benday.AzureDevOpsUtil.Api.ScriptGenerator;

/// <summary>
/// Maps the short field names a work item script uses (Title, State, Effort,
/// BacklogPriority...) to Azure DevOps reference names. Shared by
/// <see cref="CreateWorkItemsFromDataGeneratorScriptCommand"/> and
/// <see cref="CreateWorkItemsFromExcelScriptCommand"/> so a script the generator
/// exports can always be fed back through the Excel command -- the two commands
/// used to keep separate copies of this list and the Excel copy fell behind.
/// </summary>
public static class WorkItemScriptRefnames
{
    /// <summary>
    /// The pseudo-refname for a row that links the work item to its parent.
    /// It is a relation, not a field, so it is never mapped.
    /// </summary>
    public const string Parent = "PARENT";

    /// <summary>
    /// Returns the full reference name for a script row's refname. Names that
    /// are already fully qualified pass through untouched.
    /// </summary>
    /// <param name="refname">The refname as written in the script</param>
    /// <param name="processTemplateName">Process template of the target project;
    /// decides whether the backlog ordering field is BacklogPriority (Scrum) or
    /// StackRank (Agile, CMMI, Basic)</param>
    public static string GetFullRefname(string refname, string processTemplateName)
    {
        if (string.IsNullOrWhiteSpace(refname) == true)
        {
            return string.Empty;
        }

        var name = refname.Trim();

        if (Is(name, "Title"))
        {
            return "System.Title";
        }
        else if (Is(name, "State") || Is(name, "Status"))
        {
            return "System.State";
        }
        else if (Is(name, "Description"))
        {
            return "System.Description";
        }
        else if (Is(name, "IterationPath"))
        {
            return "System.IterationPath";
        }
        else if (Is(name, "AreaPath"))
        {
            return "System.AreaPath";
        }
        else if (Is(name, "Effort"))
        {
            return "Microsoft.VSTS.Scheduling.Effort";
        }
        else if (Is(name, "RemainingWork"))
        {
            return "Microsoft.VSTS.Scheduling.RemainingWork";
        }
        else if (Is(name, "BacklogPriority") || Is(name, "StackRank"))
        {
            // Both short names mean "backlog order". Which field actually
            // exists depends on the process: the Scrum family uses
            // BacklogPriority, everything else uses StackRank. The generator
            // writes BacklogPriority regardless of template, so the script
            // stays portable and the target project decides.
            return UsesBacklogPriority(processTemplateName)
                ? "Microsoft.VSTS.Common.BacklogPriority"
                : "Microsoft.VSTS.Common.StackRank";
        }
        else
        {
            return name;
        }
    }

    /// <summary>
    /// True when the process template orders its backlog with
    /// Microsoft.VSTS.Common.BacklogPriority rather than StackRank. That is the
    /// Scrum family: "Scrum", "Scrum with Backlog Refinement", and any inherited
    /// process whose name says Scrum.
    /// </summary>
    public static bool UsesBacklogPriority(string processTemplateName)
    {
        if (string.IsNullOrWhiteSpace(processTemplateName) == true)
        {
            return false;
        }

        return processTemplateName.Contains("Scrum", StringComparison.OrdinalIgnoreCase);
    }

    private static bool Is(string refname, string shortName)
    {
        return string.Equals(refname, shortName, StringComparison.OrdinalIgnoreCase);
    }
}
