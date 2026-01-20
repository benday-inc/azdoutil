using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.ProjectAdministration;

[Command(
    Category = Constants.Category_ProjectAdmin,
    Name = Constants.CommandName_DeleteProject,
        Description = "Delete team project",
        IsAsync = true)]
public class DeleteTeamProjectCommand : AzureDevOpsCommandBase
{
    public DeleteTeamProjectCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);
        arguments.AddString(Constants.ArgumentNameTeamProjectName)
            .AsRequired()
            .WithDescription("Team project name to delete");

        arguments.AddBoolean(Constants.ArgumentNameConfirm)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Confirm delete");

        return arguments;
    }

    protected override async Task OnExecute()
    {
        var projectName = Arguments[Constants.ArgumentNameTeamProjectName].Value;

        using var client = GetHttpClientInstanceForAzureDevOps();

        var project = await GetExistingTeamProject(projectName);

        var confirmed = Arguments.GetBooleanValue(Constants.ArgumentNameConfirm);

        if (project == null)
        {
            WriteLine($"Project '{projectName}' not found.");
        }
        else
        {
            if (confirmed == false)
            {
                Console.WriteLine("********");
                Console.WriteLine("********");
                Console.WriteLine("********");
                Console.WriteLine($"YOU ARE ABOUT TO DELETE THIS TEAM PROJECT!!!");

                WriteLine($"\tTeam Project Name: {project.Name}");
                Console.WriteLine("********");
                Console.WriteLine("********");
                Console.WriteLine("********");
                Console.WriteLine("Are you sure?  (yes/no)");

                var value = Console.ReadLine();

                if (value != "yes")
                {
                    Console.WriteLine("Aborting.");
                    return;
                }
                else
                {
                    await DeleteTeamProject(project.Id);
                }
            }
            else
            { 
                await DeleteTeamProject(project.Id); 
            }
        }
    }

    private async Task DeleteTeamProject(string projectId)
    {
        var requestUrl = $"_apis/projects/{projectId}?api-version=7.0";

        using var client = GetHttpClientInstanceForAzureDevOps();

        WriteLine("Calling delete...");
        await client.DeleteAsync(requestUrl);
        WriteLine("Deleted.");
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
