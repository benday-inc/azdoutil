using Benday.AzureDevOpsUtil.Api.Excel;
using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.ScriptGenerator;

[Command(Name = Constants.CommandName_CreateWorkItemsFromDataGenerator,
        Description = "Create work items using random data generator",
        IsAsync = true)]
public class CreateWorkItemsFromDataGeneratorScriptCommand : AzureDevOpsCommandBase
{
    public CreateWorkItemsFromDataGeneratorScriptCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    protected override ArgumentCollection GetAvailableArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);

        arguments.AddBoolean(Constants.CommandArg_SkipFutureDates)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Skip script steps that occur in the future");


        arguments.AddInt32(Constants.CommandArg_SprintCount)
            .WithDescription("Number of sprints to generate");
        arguments.AddString(Constants.CommandArg_TeamProjectName)
            .WithDescription("Name of the team project");
        arguments.AddString(Constants.CommandArg_ProcessTemplateName)
            .WithDescription("Process template name");
        arguments.AddBoolean(Constants.CommandArg_CreateProjectIfNotExists)
            .AsRequired()
            .AllowEmptyValue(false)
            .WithDescription("Creates the team project if it doesn't exist");

        arguments.AddString(Constants.CommandArg_SaveScriptFileTo)
            .WithDescription("Save generated script file to disk")
            .AsNotRequired();

        return arguments;
    }

    private bool _skipFutureDates = false;
    private DateTime _startDate;
    private bool _createProjectIfNotExists = false;
    private List<WorkItemScriptAction>? _actions;
    private string _teamProjectName = string.Empty;

    private DateTime FindStartDate(List<WorkItemScriptSprint> sprints)
    {
        var sprintCount = sprints.Count;

        var now = DateTime.Now;

        var proposedStart = now;

        while (proposedStart.DayOfWeek != DayOfWeek.Monday)
        {
            proposedStart = proposedStart.AddDays(-1);
        }

        var startOfSprint = proposedStart.Date.AddDays(-14 * sprintCount);

        return startOfSprint;
    }

    private void WriteScriptToDisk(WorkItemScriptGenerator generator)
    {
        if (Arguments.HasValue(Constants.CommandArg_SaveScriptFileTo) == true)
        {
            var toPath = Arguments.GetStringValue(Constants.CommandArg_SaveScriptFileTo);

            // var toPath = Path.Combine(@"c:\temp\workitemscripttemp", $"workitem-script-{DateTime.Now.Ticks}.xlsx");

            new ExcelWorkItemScriptWriter().WriteToExcel(
                toPath,
                generator.Actions);
        }        
    }

    protected override async Task OnExecute()
    {
        var sprints = new List<WorkItemScriptSprint>();

        for (int i = 0; i < Arguments.GetInt32Value(Constants.CommandArg_SprintCount); i++)
        {
            sprints.Add(new WorkItemScriptSprint()
            {
                AverageNumberOfTasksPerPbi = 3,
                NewPbiCount = 15,
                RefinedPbiCountMeeting1 = 5,
                RefinedPbiCountMeeting2 = 5,
                SprintNumber = i + 1,
                SprintPbiCount = 4,
                SprintPbisToDoneCount = 5,
                DailyHoursPerTeamMember = 6,
                TeamMemberCount = 7
            });
        }

        //var sprintStartDate = ((sprintNumber - 1) * 14);
        //var sprintEndDate = (sprintNumber * 14) - 1;

        var generator = new WorkItemScriptGenerator();

        generator.GenerateScript(sprints);

        WriteScriptToDisk(generator);

        _startDate = FindStartDate(sprints);

        _skipFutureDates = Arguments.GetBooleanValue(Constants.CommandArg_SkipFutureDates);

        if (_skipFutureDates == true)
        {
            WriteLine("Skip future dates is true. Skipping instructions that are in the future.");
        }

        _teamProjectName = Arguments.GetStringValue(Constants.CommandArg_TeamProjectName);

        _createProjectIfNotExists = Arguments.GetBooleanValue(Constants.CommandArg_CreateProjectIfNotExists);

        PopulateActions(generator);

        _skipFutureDates = Arguments.GetBooleanValue(Constants.CommandArg_SkipFutureDates);
        _createProjectIfNotExists = Arguments.GetBooleanValue(Constants.CommandArg_CreateProjectIfNotExists);

        await EnsureProjectExists();
        await PopulateIterations(sprints, _startDate);
        await RunScript();
    }

    private void PopulateActions(WorkItemScriptGenerator generator)
    {
        _actions = generator.Actions;

        CleanUpRowDataShortcuts(_actions);
    }

    private static void CleanUpRowDataShortcuts(List<WorkItemScriptAction> rows)
    {
        foreach (var row in rows)
        {
            if (string.Equals("PBI", row.Definition.WorkItemType,
                StringComparison.InvariantCultureIgnoreCase) == true)
            {
                row.Definition.WorkItemType = "Product Backlog Item";
            }
        }
    }

    private async Task RunScript()
    {
        if (_actions is null)
        {
            return;
        }

        foreach (var action in _actions)
        {
            if (action.Skip == true)
            {
                WriteLine($"Skipping action {action.ActionId} for excel row {action.Definition.ExcelRowId}");
                continue;
            }

            try
            {
                if (IsEqualCaseInsensitive(action.Definition.Operation, "Create") == true)
                {
                    await CreatePbi(action);
                }
                else if (IsEqualCaseInsensitive(action.Definition.Operation, "Update") == true)
                {
                    await UpdatePbi(action);
                }
            }
            catch (Exception)
            {
                WriteLine($"Problem while running action {action.ActionId} for excel row {action.Definition.ExcelRowId}");
                throw;
            }
        }
    }

    private static bool IsEqualCaseInsensitive(string value1, string value2)
    {
        return string.Equals(value1, value2, StringComparison.CurrentCultureIgnoreCase);
    }

    private async Task CreatePbi(WorkItemScriptAction action)
    {
        WriteLine($"Action #{action.ActionId} - Create PBI - {action.Definition.Description} - {action.Rows.Count} steps");
        await CreateWorkItem(action);
    }

    private async Task UpdatePbi(WorkItemScriptAction action)
    {
        WriteLine($"Action #{action.ActionId} - Update PBI - {action.Definition.Description} - {action.Rows.Count} steps");
        await ModifyWorkItem(action);
    }

    private async Task CreateWorkItem(WorkItemScriptAction action)
    {
        var workItemTypeName = action.Definition.WorkItemType;
        var workItemTypeNameHtmlEncoded = workItemTypeName.Replace(" ", "%20");

        var actionDate = action.GetActionDate(_startDate);

        // var requestUrl = $"{_teamProjectName}/_apis/wit/workitems/${workItemTypeNameHtmlEncoded}?api-version=6.0&bypassRules=true&supressNotifications=true";
        var requestUrl = $"{_teamProjectName}/_apis/wit/workitems/${workItemTypeNameHtmlEncoded}?api-version=6.0&supressNotifications=true";

        WorkItemFieldOperationValueCollection body = new();

        PopulateBody(action, actionDate, body);

        var savedWorkItemInfo =
            await SendPatchForBodyAndGetTypedResponse<ModifyWorkItemResponse>(
                requestUrl, body);

        WriteLine($"Modified {action.Definition.WorkItemType} for action id {action.ActionId} as work item id '{savedWorkItemInfo.Id}'...");

        AddActionWorkItemIdMap(action, savedWorkItemInfo);
    }

    private static string GetFullRefname(WorkItemScriptRow row)
    {
        if (row.Refname == "Title")
        {
            return "System.Title";
        }
        else if (
            StringUtility.IsEqualsCaseInsensitive("Status", row.Refname) ||
            StringUtility.IsEqualsCaseInsensitive("State", row.Refname))
        {
            return "System.State";
        }
        else if (row.Refname == "Effort")
        {
            return "Microsoft.VSTS.Scheduling.Effort";
        }
        else if (row.Refname == "IterationPath")
        {
            return "System.IterationPath";
        }
        else if (row.Refname == "RemainingWork")
        {
            return "Microsoft.VSTS.Scheduling.RemainingWork";
        }
        else
        {
            return row.Refname;
        }
    }

    private void PopulateBody(WorkItemScriptAction action, DateTime actionDate, WorkItemFieldOperationValueCollection body)
    {
        body.AddValue("System.ChangedDate", actionDate.ToString());

        foreach (var row in action.Rows)
        {
            if (row.Refname != "PARENT")
            {
                if (row.Refname == "IterationPath")
                {
                    body.AddValue(GetFullRefname(row), $"{_teamProjectName}\\{row.FieldValue}");
                }
                else if (row.Refname == "Status" || row.Refname == "State")
                {
                    if (row.FieldValue == "Active" && action.Definition.WorkItemType == "Product Backlog Item")
                    {
                        body.AddValue(GetFullRefname(row), $"Committed");
                    }
                    else if (row.FieldValue == "Active" && action.Definition.WorkItemType == "Task")
                    {
                        body.AddValue(GetFullRefname(row), $"In Progress");
                    }
                    else
                    {
                        body.AddValue(GetFullRefname(row), row.FieldValue);
                    }

                    if (row.FieldValue == "Done")
                    {
                        body.AddValue(
                            "Microsoft.VSTS.Common.ClosedDate",
                            actionDate.ToString());
                    }
                }
                else
                {
                    body.AddValue(GetFullRefname(row), row.FieldValue);
                }
            }
            else if (row.Refname == "PARENT")
            {
                var rel = new WorkItemRelation
                {
                    RelationType = "System.LinkTypes.Hierarchy-Reverse"
                };

                rel.Attributes.Name = "Parent";
                rel.RelationUrl = GetActionWorkItemMapUrl(row.FieldValue);

                if (Arguments.ContainsKey(Constants.CommandArg_Comment) == true &&
                    Arguments[Constants.CommandArg_Comment].HasValue == true)
                {
                    rel.Attributes.Comment = Arguments[Constants.CommandArg_Comment].Value;
                }

                body.AddValue(
                    new WorkItemFieldOperationValue()
                    {
                        Operation = "add",
                        Path = "/relations/-",
                        Value = rel
                    });
            }
        }
    }

    private readonly Dictionary<string, ModifyWorkItemResponse> _workItemIdMaps = new();

    private void AddActionWorkItemIdMap(WorkItemScriptAction action, ModifyWorkItemResponse savedWorkItemInfo)
    {
        _workItemIdMaps.Add(action.Definition.WorkItemId, savedWorkItemInfo);
    }

    private int GetActionWorkItemIdMap(WorkItemScriptAction action)
    {
        return _workItemIdMaps[action.Definition.WorkItemId].Id;
    }

    private string GetActionWorkItemMapUrl(string key)
    {
        return _workItemIdMaps[key].Url;
    }


    private async Task ModifyWorkItem(WorkItemScriptAction action)
    {
        // convert placeholder work item id from excel to 
        // the real work item that was previously created
        var realWorkItemId = GetActionWorkItemIdMap(action);

        var actionDate = action.GetActionDate(_startDate);

        // var requestUrl = $"{_teamProjectName}/_apis/wit/workitems/{realWorkItemId}?api-version=6.0&bypassRules=true&supressNotifications=true";
        var requestUrl = $"{_teamProjectName}/_apis/wit/workitems/{realWorkItemId}?api-version=6.0&supressNotifications=true";

        WorkItemFieldOperationValueCollection body = new();

        PopulateBody(action, actionDate, body);

        var savedWorkItemInfo =
            await SendPatchForBodyAndGetTypedResponse<ModifyWorkItemResponse>(
                requestUrl, body);

        WriteLine($"Modified {action.Definition.WorkItemType} for action id {action.ActionId} as work item id '{savedWorkItemInfo.Id}'...");
    }


    private GetTeamProjectCommand CreateGetTeamProjectCommandInstance()
    {
        var execInfo = ExecutionInfo.GetCloneOfArguments(
            Constants.CommandName_GetProject,
            true);

        execInfo.RemoveArgumentValue(Constants.CommandArg_TeamProjectName);
        execInfo.AddArgumentValue(Constants.ArgumentNameTeamProjectName,
            Arguments[Constants.CommandArg_TeamProjectName].Value);

        var command =
            new GetTeamProjectCommand(execInfo, _OutputProvider);

        return command;
    }

    private CreateTeamProjectCommand CreateCreateTeamProjectCommandInstance()
    {
        var execInfo = ExecutionInfo.GetCloneOfArguments(
            Constants.CommandName_CreateProject,
            true);

        execInfo.Arguments.Remove(Constants.CommandArg_TeamProjectName);
        execInfo.Arguments.Add(Constants.ArgumentNameTeamProjectName,
            Arguments[Constants.CommandArg_TeamProjectName].Value);

        execInfo.Arguments.TryAdd(Constants.CommandArg_ProcessTemplateName,
                Arguments[Constants.CommandArg_ProcessTemplateName].Value);

        var command =
            new CreateTeamProjectCommand(execInfo, _OutputProvider);

        return command;
    }

    private async Task PopulateIterations(
        List<WorkItemScriptSprint> sprints, DateTime startDate)
    {
        foreach (var item in sprints)
        {
            var execInfo = ExecutionInfo.GetCloneOfArguments(
                Constants.CommandName_SetIteration,
                true);

            var sprintStartDate = startDate.AddDays(
                ((item.SprintNumber - 1) * 14)
                );
                 
            var sprintEndDate = startDate.AddDays((item.SprintNumber * 14) - 1);

            item.StartDate = sprintStartDate;
            item.EndDate = sprintEndDate;

            WriteLine($"Setting sprint {item.SprintNumber} dates from {item.StartDate.ToShortDateString()} to {item.EndDate.ToShortDateString()}");

            execInfo.AddArgumentValue(Constants.CommandArg_IterationName, $"Sprint {item.SprintNumber}");
            execInfo.AddArgumentValue(Constants.CommandArg_StartDate, item.StartDate.ToShortDateString());
            execInfo.AddArgumentValue(Constants.CommandArg_EndDate, item.EndDate.ToShortDateString());

            var command = new SetIterationCommand(execInfo, _OutputProvider);

            await command.ExecuteAsync();
        }
    }

    private async Task EnsureProjectExists()
    {
        var getExistingProjectCommand = CreateGetTeamProjectCommandInstance();

        await getExistingProjectCommand.ExecuteAsync();

        if (_createProjectIfNotExists == false &&
            getExistingProjectCommand.LastResult == null)
        {
            throw new InvalidOperationException(
                $"Project name '{Arguments[Constants.CommandArg_TeamProjectName].Value}' does not exist.");
        }
        else if (_createProjectIfNotExists == true &&
            getExistingProjectCommand.LastResult == null)
        {
            var createProjectCommand = CreateCreateTeamProjectCommandInstance();

            await createProjectCommand.ExecuteAsync();

            Console.WriteLine($"Queued project create.  Waiting for 5 seconds...");
            await Task.Delay(5000);

            await getExistingProjectCommand.ExecuteAsync();

            if (getExistingProjectCommand.LastResult == null ||
                getExistingProjectCommand.LastResult.State != "wellFormed")
            {
                Console.WriteLine($"Project still not ready.  Waiting for 5 seconds...");
                await Task.Delay(5000);

                await getExistingProjectCommand.ExecuteAsync();

                if (getExistingProjectCommand.LastResult == null ||
                    getExistingProjectCommand.LastResult.State != "wellFormed")
                {
                    Console.WriteLine($"Project still not ready.  Waiting for 5 seconds...");
                    await Task.Delay(5000);

                    await getExistingProjectCommand.ExecuteAsync();

                    if (getExistingProjectCommand.LastResult == null ||
                        getExistingProjectCommand.LastResult.State != "wellFormed")
                    {
                        throw new InvalidOperationException("Still waiting for project.  Giving up.");
                    }
                }
            }
        }
    }
}
