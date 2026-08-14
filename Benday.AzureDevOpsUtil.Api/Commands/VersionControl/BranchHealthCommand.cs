using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.BranchHealth;
using Benday.AzureDevOpsUtil.Api.GitRemotes;
using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.VersionControl;

[Command(
    Category = Constants.Category_VersionControl,
    Name = Constants.CommandName_BranchHealth,
    IsAsync = true,
    Description =
        "Surveys the branches in a Git repository and reports how much work is in flight.")]
public class BranchHealthCommand : AzureDevOpsCommandBase
{
    public BranchHealthCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {
    }

    public BranchHealthResult? LastResult { get; private set; }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);

        arguments.AddString(Constants.ArgumentNameTeamProjectName)
            .AsNotRequired()
            .WithDescription(
                "Team project name. Read from the origin remote of the current directory's " +
                "git repository when it is not supplied.");

        arguments.AddString(Constants.ArgumentNameRepositoryName)
            .AsNotRequired()
            .WithDescription(
                "Repository name. Read from the origin remote of the current directory's " +
                "git repository when it is not supplied.");

        arguments.AddInt32(Constants.ArgumentNameActivityWindowDays)
            .WithDescription(
                "How many days count as active. Defaults to " +
                $"{BranchHealthAnalyzer.DefaultActivityWindowDays}. The last 30 days are " +
                "always reported as well.")
            .AsNotRequired();

        arguments.AddBoolean(Constants.ArgumentNameOutputCsv)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Output one row per branch as CSV instead of a report");

        return arguments;
    }

    protected override async Task OnExecute()
    {
        var projectName = GetOptionalValue(Constants.ArgumentNameTeamProjectName);
        var repositoryName = GetOptionalValue(Constants.ArgumentNameRepositoryName);
        var outputCsv = Arguments.GetBooleanValue(Constants.ArgumentNameOutputCsv);

        if (projectName.Length == 0 || repositoryName.Length == 0)
        {
            var remote = ReadCurrentRepositoryRemote();

            if (projectName.Length == 0)
            {
                projectName = remote.ProjectName;
            }

            if (repositoryName.Length == 0)
            {
                repositoryName = remote.RepositoryName;
            }

            UseConfigurationForRemote(remote);

            if (IsQuietMode == false && outputCsv == false)
            {
                WriteLine(
                    $"Using the origin remote of this directory: project '{projectName}', " +
                    $"repository '{repositoryName}' at {remote.CollectionUrl}");
                WriteLine(string.Empty);
            }
        }

        var stats = await GetBranchStats(projectName, repositoryName);

        var analyzer = new BranchHealthAnalyzer();

        var result = analyzer.Analyze(
            stats, projectName, repositoryName, DateTime.UtcNow, GetActivityWindowDays());

        LastResult = result;

        if (IsQuietMode == true)
        {
            return;
        }

        var formatter = new BranchHealthReportFormatter();

        if (outputCsv == true)
        {
            WriteLine(formatter.FormatCsv(result));
        }
        else
        {
            WriteLine(formatter.FormatReport(result));
        }
    }

    private string GetOptionalValue(string argumentName)
    {
        if (Arguments.ContainsKey(argumentName) == true &&
            Arguments[argumentName].HasValue == true)
        {
            return Arguments.GetStringValue(argumentName) ?? string.Empty;
        }

        return string.Empty;
    }

    /// <summary>
    /// Works out which repository the current directory belongs to.  Each way
    /// this can fail says something different, so each gets its own message.
    /// </summary>
    private GitRemoteInfo ReadCurrentRepositoryRemote()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        var gitDirectory = GitRepositoryLocator.FindGitDirectory(currentDirectory);

        if (gitDirectory == null)
        {
            throw new KnownException(
                $"'{currentDirectory}' is not inside a git repository, so there is nothing to " +
                $"read the repository name from. Supply /{Constants.ArgumentNameTeamProjectName} " +
                $"and /{Constants.ArgumentNameRepositoryName}.");
        }

        var remoteUrl = GitRepositoryLocator.FindRemoteUrl(currentDirectory);

        if (string.IsNullOrWhiteSpace(remoteUrl) == true)
        {
            throw new KnownException(
                "This git repository has no 'origin' remote to read the repository name from. " +
                $"Supply /{Constants.ArgumentNameTeamProjectName} and " +
                $"/{Constants.ArgumentNameRepositoryName}.");
        }

        var remote = GitRemoteUrlParser.Parse(remoteUrl);

        if (remote == null)
        {
            throw new KnownException(
                $"The origin remote of this git repository is '{remoteUrl}', which is not an " +
                $"Azure DevOps repository url. Supply /{Constants.ArgumentNameTeamProjectName} " +
                $"and /{Constants.ArgumentNameRepositoryName}.");
        }

        return remote;
    }

    /// <summary>
    /// Picks the stored configuration that talks to the collection the remote
    /// points at.  An explicit /config always wins.
    /// </summary>
    private void UseConfigurationForRemote(GitRemoteInfo remote)
    {
        if (Arguments.ContainsKey(Constants.ArgumentNameConfigurationName) == true &&
            Arguments[Constants.ArgumentNameConfigurationName].HasValue == true)
        {
            return;
        }

        var configurations = AzureDevOpsConfigurationManager.Instance.GetAll();

        var match = configurations.FirstOrDefault(x =>
            AreSameCollection(x.CollectionUrl, remote.CollectionUrl));

        if (match == null)
        {
            var known = configurations.Length == 0 ?
                "There are no configurations." :
                "Configurations: " + string.Join(
                    ", ", configurations.Select(x => $"{x.Name} ({x.CollectionUrl})")) + ".";

            throw new KnownException(
                $"The origin remote points at {remote.CollectionUrl}, and no azdoutil " +
                $"configuration uses that url. {known} Add one with " +
                $"{Constants.CommandArgumentNameAddUpdateConfig}, or name a configuration with " +
                $"/{Constants.ArgumentNameConfigurationName}.");
        }

        Configuration = match;
    }

    private static bool AreSameCollection(string? left, string? right)
    {
        var trimmedLeft = (left ?? string.Empty).TrimEnd('/');
        var trimmedRight = (right ?? string.Empty).TrimEnd('/');

        return string.Equals(trimmedLeft, trimmedRight, StringComparison.OrdinalIgnoreCase);
    }

    private int GetActivityWindowDays()
    {
        if (Arguments.ContainsKey(Constants.ArgumentNameActivityWindowDays) == true &&
            Arguments[Constants.ArgumentNameActivityWindowDays].HasValue == true)
        {
            var value = Arguments.GetInt32Value(Constants.ArgumentNameActivityWindowDays);

            if (value > 0)
            {
                return value;
            }
        }

        return BranchHealthAnalyzer.DefaultActivityWindowDays;
    }

    /// <summary>
    /// One call covers every branch in the repository.  This endpoint is known
    /// to be slow on repositories with hundreds of branches.
    /// </summary>
    private async Task<List<GitBranchStatsInfo>> GetBranchStats(
        string projectName, string repositoryName)
    {
        using var client = GetHttpClientInstanceForAzureDevOps();

        var requestUrl =
            $"{Uri.EscapeDataString(projectName)}/_apis/git/repositories/" +
            $"{Uri.EscapeDataString(repositoryName)}/stats/branches?api-version=7.0";

        var response = await client.GetAsync(requestUrl);

        if (response.IsSuccessStatusCode == false)
        {
            var body = await response.Content.ReadAsStringAsync();

            var message = TfvcAssessment.AzureDevOpsErrorMessageReader.GetMessageOrDefault(
                body, $"{(int)response.StatusCode} {response.ReasonPhrase}");

            throw new KnownException(
                $"Could not read the branches of '{repositoryName}' in team project " +
                $"'{projectName}'. {message}");
        }

        var json = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(json) == true)
        {
            return new List<GitBranchStatsInfo>();
        }

        var parsed = JsonSerializer.Deserialize<GitBranchStatsListResponse>(
            json, JsonUtilities.DefaultOptions);

        return parsed?.Value ?? new List<GitBranchStatsInfo>();
    }
}
