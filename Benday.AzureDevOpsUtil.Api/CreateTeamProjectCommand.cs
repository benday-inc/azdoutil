using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;


[Command(Name = Constants.CommandName_CreateProject,
        Description = "List team projects",
        IsAsync = true)]
public class CreateTeamProjectCommand : AzureDevOpsCommandBase
{
    public CreateTeamProjectCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public ListProjectsResponse? LastResult { get; private set; }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);
        arguments.AddString(Constants.ArgumentNameTeamProjectName)
            .AsRequired()
            .WithDescription("Team project name");
        arguments.AddString(Constants.CommandArg_ProcessTemplateName)
            .WithDescription("Process template name");

        return arguments;
    }

    protected override async Task OnExecute()
    {
        var projectName = Arguments[Constants.ArgumentNameTeamProjectName].Value;

        var project = await GetExistingTeamProject(projectName);

        if (project == null)
        {
            var processTemplateName = Arguments[Constants.CommandArg_ProcessTemplateName].Value;

            var processTemplate = await GetProcessTemplate(
                processTemplateName);

            if (processTemplate == null)
            {
                throw new InvalidOperationException(
                    $"Invalid process template name '{processTemplateName}'.");
            }

            await CreateNewTeamProject(projectName, processTemplate);
        }
        else
        {
            throw new InvalidOperationException(
                $"Team project with name '{project.Name}' already exists.");
        }
    }

    private async Task<TeamProjectInfo?> GetExistingTeamProject(string teamProjectName)
    {
        var execInfo = ExecutionInfo.GetCloneOfArguments(
            Constants.CommandName_ListProjects,
            true);

        var command = new ListTeamProjectsCommand(execInfo, _OutputProvider);

        await command.ExecuteAsync();

        if (command.LastResult == null || command.LastResult.Count == 0)
        {
            return null;
        }
        else
        {
            var returnValue = command.LastResult.Projects.Where(x =>
                string.Equals(x.Name, teamProjectName, StringComparison.CurrentCultureIgnoreCase))
                .FirstOrDefault();

            return returnValue;
        }
    }

    private async Task<ProcessTemplateInfo?> GetProcessTemplate(string processTemplateName)
    {
        var args = ExecutionInfo.GetCloneOfArguments(Constants.CommandName_ListProjects, true);
        var command = new ListProcessTemplatesCommand(args, _OutputProvider);

        await command.ExecuteAsync();

        if (command.LastResult == null || command.LastResult.Count == 0)
        {
            return null;
        }
        else
        {
            var returnValue = command.LastResult.Values.Where(x =>
                string.Equals(x.Name, processTemplateName, StringComparison.CurrentCultureIgnoreCase))
                .FirstOrDefault();

            return returnValue;
        }
    }

    private async Task CreateNewTeamProject(string name, ProcessTemplateInfo processTemplate)
    {
        CreateTeamProjectRequest request = new();
        request.Capabilities = new();
        request.Capabilities.ProcessTemplate = new();
        request.Capabilities.VersionControl = new();

        request.Name = name;
        request.Description = name;
        request.Capabilities.VersionControl.SourceControlType = "Git";
        request.Capabilities.ProcessTemplate.TemplateTypeId = processTemplate.Id;

        var requestUrl = $"_apis/projects?api-version=7.0";

        var response = await SendPostForBodyAndGetTypedResponseSingleAttempt<CreateTeamProjectResponse, CreateTeamProjectRequest>(
            requestUrl, request);

        if (IsQuietMode == false)
        {
            if (response != null)
            {
                WriteLine($"Status: {response.Status}");
                WriteLine($"Url: {response.Url}");
                WriteLine($"Id: {response.Id}");
            }
            else
            {
                WriteLine($"Response was null");
            }
        }
    }
}
