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

    public override ArgumentCollection GetArguments()
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

                if (project.DefaultTeam == null)
                {
                    WriteLine($"Default team: (n/a)");
                }
                else
                {
                    WriteLine($"Default team name: {project.DefaultTeam.Name}");
                    WriteLine($"Default team id: {project.DefaultTeam.Id}");
                }

                if (project.Capabilities == null)
                {
                    WriteLine($"Capabilities: (n/a)");
                }
                else
                {
                    WriteLine($"Process Template Id: {project.Capabilities.ProcessTemplate.TemplateTypeId}");
                    WriteLine($"Process Template Name: {project.Capabilities.ProcessTemplate.Name}");
                    WriteLine($"Source Control Type: {project.Capabilities.VersionControl.SourceControlType}");
                    WriteLine($"Git Enabled: {project.Capabilities.VersionControl.GitEnabled}");
                    WriteLine($"TFVC Enabled: {project.Capabilities.VersionControl.TfvcEnabled}");
                }
            }
        }
    }

    private async Task<TeamProjectInfo?> GetExistingTeamProject(string teamProjectName)
    {
        var teamProjectNameEncoded = teamProjectName.Replace(" ", "%20");

        var requestUrl = $"_apis/projects/{teamProjectNameEncoded}?api-version=7.0&includeCapabilities=true";

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
