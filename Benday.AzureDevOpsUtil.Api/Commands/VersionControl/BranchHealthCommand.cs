using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.BranchHealth;
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
            .AsRequired()
            .WithDescription("Team project name");

        arguments.AddString(Constants.ArgumentNameRepositoryName)
            .AsRequired()
            .WithDescription("Repository name");

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
        var projectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        var repositoryName = Arguments.GetStringValue(Constants.ArgumentNameRepositoryName);
        var outputCsv = Arguments.GetBooleanValue(Constants.ArgumentNameOutputCsv);

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
