using Benday.AzureDevOpsUtil.Api.Excel;
using Benday.CommandsFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.AzureDevOpsUtil.Api;

[Command(Name = Constants.CommandName_CreateWorkItemsFromExcelScript,
        Description = "Create work items using Excel script",
        IsAsync = true)]
public class CreateWorkItemsFromExcelScriptCommand : AzureDevOpsCommandBase
{
    public CreateWorkItemsFromExcelScriptCommand(
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


        arguments.AddString(Constants.CommandArg_PathToExcel)
            .WithDescription("Path to the Excel script");
        arguments.AddDateTime(Constants.CommandArg_StartDate)
            .WithDescription("Date for the start of the Excel script");
        arguments.AddString(Constants.CommandArg_TeamProjectName)
            .WithDescription("Name of the team project");
        arguments.AddString(Constants.CommandArg_ProcessTemplateName)
            .WithDescription("Process template name");
        arguments.AddBoolean(Constants.CommandArg_CreateProjectIfNotExists)
            .AsRequired()
            .AllowEmptyValue(false)
            .WithDescription("Creates the team project if it doesn't exist");

        return arguments;
    }

    private bool _skipFutureDates = false;
    private DateTime _startDate;
    private bool _createProjectIfNotExists = false;
    private string _pathToExcel;
    private List<WorkItemScriptAction> _actions;

    protected override async Task OnExecute()
    {
        _skipFutureDates = ArgumentBooleanValue(Constants.CommandArg_SkipFutureDates);
        _createProjectIfNotExists = ArgumentBooleanValue(Constants.CommandArg_CreateProjectIfNotExists);

        await EnsureProjectExists();
        await PopulateIterations();
        await RunScript();
    }

    private GetTeamProjectCommand CreateGetTeamProjectCommandInstance()
    {
        var execInfo = ExecutionInfo.GetCloneOfArguments(
            Constants.CommandName_GetProject,
            true);

        execInfo.Arguments.Remove(Constants.CommandArg_TeamProjectName);
        execInfo.Arguments.Add(Constants.ArgumentNameTeamProjectName,
            Arguments[Constants.CommandArg_TeamProjectName].Value);

        var command = 
            new GetTeamProjectCommand(execInfo, _OutputProvider);

        return command;
    }

    private async Task PopulateIterations()
    {
        var reader = new ExcelWorkItemIterationRowReader(
                        new ExcelReader(
                            _pathToExcel));

        var rows = reader.GetRows();

        foreach (var item in rows)
        {
            var args = Arguments.GetCloneOfArguments(true);
            args.SetArgumentValue(Constants.CommandArg_IterationName, item.IterationName);
            args.SetArgumentValue(Constants.CommandArg_StartDate, item.GetIterationStart(_startDate).ToShortDateString());
            args.SetArgumentValue(Constants.CommandArg_EndDate, item.GetIterationEnd(_startDate).ToShortDateString());

            var command = new SetIterationCommand(args);

            await command.Run();
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
            var createProjectArgs = args.GetCloneOfArguments(true);

            createProjectArgs.SetArgumentValue(Constants.CommandArg_ProcessTemplateName,
                Arguments[Constants.CommandArg_ProcessTemplateName]);

            var createProjectCommand = new CreateTeamProjectCommand(createProjectArgs);

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
