using System.Runtime.InteropServices;

using Benday.AzureDevOpsUtil.Api.Commands.ProjectAdministration;
using Benday.AzureDevOpsUtil.Api.Excel;
using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

using Benday.AzureDevOpsUtil.Api.Commands.WorkItems;
namespace Benday.AzureDevOpsUtil.Api.ScriptGenerator;

[Command(
    Category = Constants.Category_TestData,
    Name = Constants.CommandName_CreateWorkItemsFromDataGenerator,
        Description = "Create work items using random data generator",
        IsAsync = true)]
public class CreateWorkItemsFromDataGeneratorScriptCommand : AzureDevOpsCommandBase
{
    public CreateWorkItemsFromDataGeneratorScriptCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
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

        arguments.AddInt32(Constants.CommandArg_TeamCount)
            .AsNotRequired()
            .AllowEmptyValue(false)
            .WithDescription("Creates data for multiple teams. This option is only available when creating a new project.");

        arguments.AddBoolean(Constants.CommandArg_AllPbisGoToDone)
            .AsNotRequired()
            .AllowEmptyValue(true)
            .WithDescription("All PBIs in a sprint makes it to done");

        arguments.AddBoolean(Constants.CommandArg_AddSessionTag)
            .AsNotRequired()
            .AllowEmptyValue(true)
            .WithDescription("Add a session tag to work items");

        arguments.AddString(Constants.CommandArg_SaveScriptFileTo)
            .WithDescription("Save generated script file to disk in this directory. Note the filename will be auto-generated.")
            .AsNotRequired();

        arguments.AddBoolean(Constants.CommandArg_ScriptOnly)
          .AsNotRequired()
          .AllowEmptyValue(true)
          .WithDescription($"Creates the excel export script. Requires an arg value for '{Constants.CommandArg_SaveScriptFileTo}'");

        return arguments;
    }

    private bool _skipFutureDates = false;
    private DateTime _startDate;
    private bool _createProjectIfNotExists = false;
    private List<WorkItemScriptAction>? _actions;
    private string _teamProjectName = string.Empty;

    private bool _addSessionTag = false;

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

    private string? WriteScriptToDisk(WorkItemScriptGenerator generator)
    {
        if (Arguments.HasValue(Constants.CommandArg_SaveScriptFileTo) == true)
        {
            var toPath = Arguments.GetStringValue(Constants.CommandArg_SaveScriptFileTo);

            toPath = Path.Combine(toPath, $"workitem-script-{DateTime.Now.Ticks}.xlsx");

            // var toPath = Path.Combine(@"c:\temp\workitemscripttemp", $"workitem-script-{DateTime.Now.Ticks}.xlsx");

            new ExcelWorkItemScriptWriter().WriteToExcel(
                toPath,
                generator.Actions);

            return toPath;
        }
        else
        {
            return null;
        }
    }

    private readonly string _sessionId = DateTime.Now.Ticks.ToString()[^5..];

    protected override async Task OnExecute()
    {
        var scriptOnly = Arguments.GetBooleanValue(Constants.CommandArg_ScriptOnly);

        if (scriptOnly == true && Arguments[Constants.CommandArg_SaveScriptFileTo].HasValue == false)
        {
            throw new KnownException($"When running in script only mode, a value for '{Constants.CommandArg_SaveScriptFileTo}' is required.");
        }

        var processTemplateName = Arguments.GetStringValue(Constants.CommandArg_ProcessTemplateName);

        bool useScrumWithBacklogRefinement;

        if (processTemplateName.Equals("Scrum", StringComparison.CurrentCultureIgnoreCase) == true)
        {
            useScrumWithBacklogRefinement = false;
        }
        else if (processTemplateName.Equals("Scrum with Backlog Refinement", StringComparison.CurrentCultureIgnoreCase) == true)
        {
            useScrumWithBacklogRefinement = true;
        }
        else
        {
            throw new KnownException($"Process template '{processTemplateName}' not supported. Work item script generation only supported for 'Scrum' or 'Scrum with Backlog Refinement'.");
        }

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

        var markAllPbisAsDone = Arguments.GetBooleanValue(
            Constants.CommandArg_AllPbisGoToDone);

        _addSessionTag = Arguments.GetBooleanValue(
            Constants.CommandArg_AddSessionTag);

        var generator = new WorkItemScriptGenerator(useScrumWithBacklogRefinement);

        generator.GenerateScript(sprints, markAllPbisAsDone);

        var outputPathAndFileName = WriteScriptToDisk(generator);

        if (scriptOnly == true)
        {
            WriteLine("Running in script-only mode. Skipping write to Azure DevOps.");
            WriteLine($"Script written to '{outputPathAndFileName}'");
        }
        else
        {
            _startDate = FindStartDate(sprints);

            _skipFutureDates = Arguments.GetBooleanValue(Constants.CommandArg_SkipFutureDates);

            if (_skipFutureDates == true)
            {
                WriteLine("Skip future dates is true. Skipping instructions that are in the future.");
            }

            _teamProjectName = Arguments.GetStringValue(Constants.CommandArg_TeamProjectName);

            _createProjectIfNotExists = Arguments.GetBooleanValue(Constants.CommandArg_CreateProjectIfNotExists);

            WriteLine($"Team project name: '{_teamProjectName}'");
            WriteLine($"Create project if not exists: '{_createProjectIfNotExists}'");

            if (_createProjectIfNotExists == false && Arguments.HasValue(Constants.CommandArg_TeamCount) == true)
            {
                throw new KnownException($"When '{Constants.CommandArg_CreateProjectIfNotExists}' is false, '{Constants.CommandArg_TeamCount}' is not allowed.");
            }
            else if (_createProjectIfNotExists == false ||
                Arguments.HasValue(Constants.CommandArg_TeamCount) == false ||
                Arguments.GetInt32Value(Constants.CommandArg_TeamCount) < 2)
            {
                WriteLine("Populating data for a single team.");

                PopulateActions(generator);

                _skipFutureDates = Arguments.GetBooleanValue(Constants.CommandArg_SkipFutureDates);
                _createProjectIfNotExists = Arguments.GetBooleanValue(Constants.CommandArg_CreateProjectIfNotExists);

                await EnsureProjectExists();
                await PopulateIterations(sprints, _startDate);
                await RunScript();

                if (outputPathAndFileName != null)
                {
                    WriteLine($"Script written to '{outputPathAndFileName}'");
                }

                WriteLine($"Done.");
            }
            else if (_createProjectIfNotExists == true && Arguments.HasValue(Constants.CommandArg_TeamCount) == true &&
                Arguments.GetInt32Value(Constants.CommandArg_TeamCount) > 2)
            {
                WriteLine("Populating data for multiple teams.");

                await PopulateForMultipleSprints(sprints, _startDate, outputPathAndFileName, useScrumWithBacklogRefinement, markAllPbisAsDone);

            }
            else
            {
                throw new KnownException("Unsupported combination of arguments.");
            }

        }
    }
    private async Task PopulateForMultipleSprints(List<WorkItemScriptSprint> sprints, DateTime startDate, string? outputPathAndFileName, bool useScrumWithBacklogRefinement, bool markAllPbisAsDone)
    {
        var teamCount = Arguments.GetInt32Value(Constants.CommandArg_TeamCount);

        var projectInfo = await EnsureProjectExists();

        if (projectInfo == null)
        {
            throw new InvalidOperationException($"Failed to create project '{_teamProjectName}'.");
        }

        await PopulateIterations(sprints, startDate);

        for (var i = 0; i < teamCount; i++)
        {
            WriteLine($"Generating script for team {i + 1} of {teamCount}...");

            _workItemIdMaps.Clear();

            var generator = new WorkItemScriptGenerator(useScrumWithBacklogRefinement);

            generator.GenerateScript(sprints, markAllPbisAsDone);

            PopulateActions(generator, $"{projectInfo.Name}\\Team {i + 1}");

            _skipFutureDates = Arguments.GetBooleanValue(Constants.CommandArg_SkipFutureDates);

            await RunScript();

            if (outputPathAndFileName != null)
            {
                WriteLine($"Script written to '{outputPathAndFileName}'");
            }

            WriteLine($"Generated script for team {i + 1} of {teamCount}...");
        }


        WriteLine($"Done.");
    }

    private void PopulateActions(WorkItemScriptGenerator generator)
    {
        _actions = generator.Actions;

        CleanUpRowDataShortcuts(_actions);
    }

    private void PopulateActions(WorkItemScriptGenerator generator, string assignToTeamName)
    {
        _actions = generator.Actions;

        CleanUpRowDataShortcuts(_actions);

        foreach (var action in _actions)
        {
            if (IsEqualCaseInsensitive(action.Definition.Operation, "Create") == true)
            {
                WriteLine($"Assigning new PBI to team '{assignToTeamName}'.");
                action.SetValue("System.AreaPath", assignToTeamName);
            }
        }
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
        await CreateWorkItem(action, true);
    }

    private async Task UpdatePbi(WorkItemScriptAction action)
    {
        await ModifyWorkItem(action, true);
    }

    private async Task CreateWorkItem(WorkItemScriptAction action, bool bypassRules)
    {
        // WriteLine($"Action #{action.ActionId} - Create PBI - {action.Definition.Description} - {action.Rows.Count} steps");

        var workItemTypeName = action.Definition.WorkItemType;
        var workItemTypeNameHtmlEncoded = workItemTypeName.Replace(" ", "%20");

        var actionDate = action.GetActionDate(_startDate);

        string requestUrl;

        if (bypassRules == true)
        {
            requestUrl = $"{_teamProjectName}/_apis/wit/workitems/${workItemTypeNameHtmlEncoded}?api-version=6.0&bypassRules=true&supressNotifications=true";
        }
        else
        {
            requestUrl = $"{_teamProjectName}/_apis/wit/workitems/${workItemTypeNameHtmlEncoded}?api-version=6.0";
        }

        WorkItemFieldOperationValueCollection body = new();

        PopulateBody(action, actionDate, body);

        body.AddValue("System.CreatedDate", actionDate.ToString());

        if (_addSessionTag == true)
        {
            body.AddValue("System.Tags", $"Session {_sessionId}");
        }

        var savedWorkItemInfo =
            await SendPatchForBodyAndGetTypedResponse<ModifyWorkItemResponse>(
                requestUrl, body);

        WriteLine($"Create PBI - {action.Definition.Description} -- {action.Definition.WorkItemType} for action id {action.ActionId} as work item id '{savedWorkItemInfo.Id}'...");

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
                else if (StringUtility.IsEqualsCaseInsensitive("State", row.Refname) ||
                    StringUtility.IsEqualsCaseInsensitive("Status", row.Refname)
                    )
                {
                    if (row.FieldValue == "Active" && action.Definition.WorkItemType == "Product Backlog Item")
                    {
                        body.AddValue(GetFullRefname(row), $"Committed");
                    }
                    else if (row.FieldValue == "Active" && action.Definition.WorkItemType == "Task")
                    {
                        body.AddValue(GetFullRefname(row), $"In Progress");
                    }
                    else if (row.FieldValue == "Done")
                    {
                        body.AddValue(GetFullRefname(row), row.FieldValue);
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


    private async Task ModifyWorkItem(WorkItemScriptAction action, bool bypassRules)
    {
        // WriteLine($"Action #{action.ActionId} - Update PBI - {action.Definition.Description} - {action.Rows.Count} steps");

        // convert placeholder work item id from excel to 
        // the real work item that was previously created
        var realWorkItemId = GetActionWorkItemIdMap(action);

        var actionDate = action.GetActionDate(_startDate);

        string requestUrl;

        if (bypassRules == true)
        {
            requestUrl =
                $"{_teamProjectName}/_apis/wit/workitems/{realWorkItemId}?api-version=6.0&bypassRules=true&supressNotifications=true";
        }
        else
        {
            requestUrl = $"{_teamProjectName}/_apis/wit/workitems/{realWorkItemId}?api-version=6.0";
        }

        WorkItemFieldOperationValueCollection body = new();

        PopulateBody(action, actionDate, body);

        var savedWorkItemInfo =
            await SendPatchForBodyAndGetTypedResponse<ModifyWorkItemResponse>(
                requestUrl, body);

        WriteLine($"Update PBI - {action.Definition.Description} -- {action.Definition.WorkItemType} for action id {action.ActionId} as work item id '{savedWorkItemInfo.Id}'...");
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

    private async Task<TeamProjectInfo?> EnsureProjectExists()
    {
        WriteLine($"Team project: {_teamProjectName}");

        bool createdNewProject = false;

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
            createdNewProject = true;

            var createProjectCommand = CreateCreateTeamProjectCommandInstance();

            await createProjectCommand.ExecuteAsync();

            WriteLine($"Queued project create.  Waiting for 5 seconds...");
            await Task.Delay(5000);

            await getExistingProjectCommand.ExecuteAsync();

            if (getExistingProjectCommand.LastResult == null ||
                getExistingProjectCommand.LastResult.State != "wellFormed")
            {
                WriteLine($"Project still not ready.  Waiting for 5 seconds...");
                await Task.Delay(5000);

                await getExistingProjectCommand.ExecuteAsync();

                if (getExistingProjectCommand.LastResult == null ||
                    getExistingProjectCommand.LastResult.State != "wellFormed")
                {
                    WriteLine($"Project still not ready.  Waiting for 5 seconds...");
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

        if (createdNewProject == true)
        {
            // wait before proceeding to let azdo catch up
            WriteLine($"Project ready.  Calling backlog board to initialize...");

            using var client = GetHttpClientInstanceForAzureDevOps();

            await client.GetStringAsync($"{_teamProjectName}/_boards/board/t/{_teamProjectName}%20Team");

            WriteLine($"Project ready.  Called backlog board to initialize...");

            await CreateTeams(getExistingProjectCommand.LastResult!);
        }

        return getExistingProjectCommand.LastResult;
    }

    private async Task CreateTeams(TeamProjectInfo lastResult)
    {
        if (Arguments.HasValue(Constants.CommandArg_TeamCount) == false)
        {
            return;
        }
        else
        {
            var teamCount = Arguments.GetInt32Value(Constants.CommandArg_TeamCount);
            if (teamCount < 2)
            {
                WriteLine($"Team count is less than 2.  Skipping team creation.");

                return;
            }
            else
            {
                WriteLine($"Creating {teamCount} teams...");

                for (int i = 0; i < teamCount; i++)
                {
                    await CreateTeam(lastResult.Name, $"Team {i + 1}");
                }
            }
        }
    }

    private async Task<TeamInfo> CreateTeam(string teamProjectName, string teamName)
    {
        var args = ExecutionInfo.GetCloneOfArguments(
                        Constants.CommandArgumentName_CreateTeam,
                        true);

        args.AddArgumentValue(Constants.ArgumentNameTeamProjectName, teamProjectName);
        args.AddArgumentValue(Constants.ArgumentNameTeamName, teamName);

        var command = new CreateTeamCommand(
            args, _OutputProvider);

        await command.ExecuteAsync();

        if (command.LastResult == null)
        {
            throw new KnownException($"Could not team '{teamName}' in team project.");
        }
        else
        {
            WriteLine($"Created team '{teamName}' in team project.");
        }

        return command.LastResult;
    }
}
