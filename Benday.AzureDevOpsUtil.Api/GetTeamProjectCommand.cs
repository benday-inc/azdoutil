using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(Name = Constants.CommandName_GetProject,
        Description = "Get team project info",
        IsAsync = true)]
public class GetTeamProjectCommand : AzureDevOpsCommandBase
{
    public GetTeamProjectCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    protected override ArgumentCollection GetAvailableArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);
        arguments.AddString(Constants.ArgumentNameTeamProjectName)
            .AsRequired()
            .WithDescription("Team project name");

        return arguments;
    }

    protected override async Task OnExecute()
    {
        var projectName = Arguments[Constants.ArgumentNameTeamProjectName].Value;

        using var client = GetHttpClientInstanceForAzureDevOps();

        var project = await GetExistingTeamProject(projectName);

        if (IsQuietMode == false)
        {
            if (project == null)
            {
                WriteLine($"Project '{projectName}' not found.");
            }
            else
            {
                WriteLine($"Name: {project.Name}");
                WriteLine($"Id: {project.Id}");
                WriteLine($"Url: {project.Url}");
                WriteLine($"LastUpdateTime: {project.LastUpdateTime}");
                WriteLine($"State: {project.State}");
            }
        }
    }

    private async Task<TeamProjectInfo?> GetExistingTeamProject(string teamProjectName)
    {
        var teamProjectNameEncoded = teamProjectName.Replace(" ", "%20");

        var requestUrl = $"_apis/projects/{teamProjectNameEncoded}?api-version=7.0";

        try
        {
            var result = await CallEndpointViaGetAndGetResult<TeamProjectInfo>(requestUrl, false, false);

            LastResult = result;

            return result;
        }
        catch (Exception)
        {
            LastResult = null;
            return null;
        }

    }

    public TeamProjectInfo? LastResult { get; set; }
}
