using System.Text.Json;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(Name = Constants.CommandArgumentName_ListGitRepos,
    IsAsync = true,
    Description = "Gets list of Git repositories from an Azure DevOps Team Project.")]
public class ListGitRepositoriesForProjectCommand : AzureDevOpsCommandBase
{
    public GitRepositoryInfo[]? LastResult { get; private set; }

    public ListGitRepositoriesForProjectCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        AddCommonArguments(args);
        args.AddString(Constants.ArgumentNameTeamProjectName).AsRequired().
            WithDescription("Team project name that contains the git repositories");      

        return args;
    }

    protected override async Task OnExecute()
    {
        var projectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);

        var result = await GetGitRepositories(projectName);

        LastResult = result;

        if (IsQuietMode)
        {
            return;
        }
        else if (result == null)
        {
            WriteLine("Result is null");
        }
        else if (result.Length == 0)
        {
            WriteLine("Result length is 0.");
        }
        else
        {
            WriteLine($"Repository count: {result.Length}");

            foreach (var item in result)
            {
                WriteLine($"{item.Name} ({item.Id}): {item.WebUrl}");
            }
        }
    }

    public async Task<GitRepositoryInfo[]?> GetGitRepositories(string projectName)
    {
        if (string.IsNullOrEmpty(projectName))
        {
            throw new ArgumentException($"{nameof(projectName)} is null or empty.", nameof(projectName));
        }

        var projectNameUrlEncoded = HttpUtility.UrlEncode(projectName);

        using var client = GetHttpClientInstanceForAzureDevOps();

        var results = await client.GetAsync($"{projectName}/_apis/git/repositories");

        if (results.IsSuccessStatusCode == false)
        {
            throw new InvalidOperationException($"Request failed -- {results.StatusCode} {results.ReasonPhrase}");
        }

        var content = await results.Content.ReadAsStringAsync();

        var objectResults = JsonSerializer.Deserialize<GitRepositoryList>(content);

        return objectResults?.Value;
    }
}
