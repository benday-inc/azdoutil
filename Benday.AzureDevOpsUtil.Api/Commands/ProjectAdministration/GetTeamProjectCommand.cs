using System.Text.Json;
using System.Text.Json.Nodes;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;
using Benday.Common.Json;

namespace Benday.AzureDevOpsUtil.Api.Commands.ProjectAdministration;

[Command(
    Category = Constants.Category_ProjectAdmin,
    Name = Constants.CommandName_GetProject,
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

        arguments
            .AddString(Constants.ArgumentNameTeamProjectName)
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
                await GetProjectCategories(project);


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

                if (project.Categories == null || project.Categories.Count == 0)
                {
                    WriteLine($"Categories: (n/a)");
                }
                else
                {
                    WriteLine($"Categories:");
                    foreach (var category in project.Categories)
                    {
                        WriteLine($"  {category.Key}: {category.Value}");
                    }
                }
            }
        }
    }

    private async Task GetProjectCategories(TeamProjectInfo project)
    {
        var requestUrl = $"{project.Id}/_apis/wit/workitemtypecategories?api-version=7.1";
        
        var result = await GetStringAsync(requestUrl, false, false);

        if (result == null)
        {
            WriteLine(
                $"Unable to get project properties for project '{project.Name}' (id: {project.Id}).");
            return;
        }
        else
        {
            var doc = JsonDocument.Parse(result);

            if (doc == null)
            {
                WriteLine(
                    $"Unable to parse categories for project '{project.Name}' (id: {project.Id}).");
                return;
            }

            var element = doc.RootElement;

            var categories = element.GetProperty("value").EnumerateArray();
                        
            foreach (var category in categories) {
                var categoryName =
                    category.SafeGetString("referenceName");
                var categoryWorkItemType = category.SafeGetString("defaultWorkItemType", "name");

                project.Categories[categoryName] = categoryWorkItemType;
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
