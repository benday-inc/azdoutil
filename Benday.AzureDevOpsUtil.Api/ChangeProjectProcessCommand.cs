using System.Web;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(Name = Constants.CommandName_ChangeProjectProcess,
        Description = "Change the process for a Team Project",
        IsAsync = true)]
public class ChangeProjectProcessCommand : AzureDevOpsCommandBase
{
    public ChangeProjectProcessCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);
        arguments.AddString(Constants.ArgumentNameTeamProjectName)
            .AsRequired()
            .WithDescription("Team project name");

        arguments.AddString(Constants.ArgumentNameProcessName)
            .AsRequired()
            .WithDescription("New process name");

        return arguments;
    }

    private async Task<ProcessTemplateDetailInfo> GetProcessTemplate(string processName)
    {
        var listProcessTemplates = new ListProcessTemplatesCommand(
            ExecutionInfo.GetCloneOfArguments(
               Constants.CommandName_ListProcessTemplates, true),
            _OutputProvider);

        await listProcessTemplates.ExecuteAsync();

        var processTemplates = listProcessTemplates.LastResult;

        if (processTemplates == null || processTemplates.Count == 0)
        {
            throw new KnownException("No process templates available on the server.");
        }
        else
        {
            var match = processTemplates.Values.Where(x =>
                string.Equals(x.Name,
                processName,
                StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

            if (match == null)
            {
                throw new KnownException(
                    $"Process template {processName} does not exist.");
            }
            else
            {
                return match;
            }
        }
    }

    private async Task<TeamProjectInfo> GetTeamProject(string projectName)
    {
        var getTeamProject = new GetTeamProjectCommand(
            ExecutionInfo.GetCloneOfArguments(
               Constants.CommandName_GetProject, true),
            _OutputProvider);

        await getTeamProject.ExecuteAsync();

        var result = getTeamProject.LastResult;

        if (result == null)
        {
            throw new KnownException($"Could not find team project '{projectName}'.");
        }
        else
        {
            return result;
        }
    }

    protected override async Task OnExecute()
    {
        var projectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        var processName = Arguments.GetStringValue(Constants.ArgumentNameProcessName);

        var project = await GetTeamProject(projectName);
        var process = await GetProcessTemplate(processName);

        using var client = GetHttpClientInstanceForAzureDevOps();

        var requestUrl = "_apis/work/processAdmin/processes/QueuePromoteProjectToProcessJob" +
            $"?projectName={HttpUtility.UrlEncode(project.Name)}" +
            "&api-version=7.0" +
            $"&targetProcessId={process.Id}";

        var response = await client.PostAsync(requestUrl, null);

        if (response != null)
        {
            response.EnsureSuccessStatusCode();

            WriteLine("Change queued. This may take a minute or so to be reflected in Azure DevOps.");
        }
        else
        {
            throw new InvalidOperationException($"Response from server was null.");
        }
    }
}