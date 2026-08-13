using Benday.AzureDevOpsUtil.Api.TfvcAssessment;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.VersionControl;

[Command(
    Category = Constants.Category_VersionControl,
    Name = Constants.CommandName_AssessTfvcMigration,
    IsAsync = true,
    Description =
        "Analyzes a TFVC path and reports what a conversion to Git would have to deal with.")]
public class AssessTfvcMigrationCommand : AzureDevOpsCommandBase
{
    public AssessTfvcMigrationCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {
    }

    public TfvcAssessmentResult? LastResult { get; private set; }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);

        arguments.AddString(Constants.ArgumentNameTeamProjectName)
            .AsRequired()
            .WithDescription("Team project name");

        arguments.AddString(Constants.ArgumentNameTfvcFolder)
            .AsNotRequired()
            .WithDescription(
                "TFVC path to assess. Defaults to $/<teamproject>.");

        arguments.AddInt32(Constants.ArgumentNameScanDepth)
            .WithDescription(
                "How many folder levels below the path to scan for unregistered branches. " +
                $"Defaults to {TfvcFolderHeuristicScanner.DefaultMaxDepth}.")
            .AsNotRequired();

        arguments.AddBoolean(Constants.ArgumentNameOutputCsv)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Output the findings as CSV instead of a report");

        return arguments;
    }

    protected override async Task OnExecute()
    {
        var projectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);

        var tfvcPath = GetTfvcPath(projectName);

        if (tfvcPath.StartsWith("$/", StringComparison.Ordinal) == false)
        {
            throw new KnownException(
                $"The value for /{Constants.ArgumentNameTfvcFolder} should start with '$/'.  " +
                $"For example: $/{projectName}/Main");
        }

        var outputCsv = Arguments.GetBooleanValue(Constants.ArgumentNameOutputCsv);

        var analyzer = new TfvcAssessmentAnalyzer(CreateApiClient())
        {
            MaxScanDepth = GetScanDepth()
        };

        if (IsQuietMode == false && outputCsv == false)
        {
            analyzer.ProgressCallback = message => WriteLine(message);
        }

        var result = await analyzer.AnalyzeAsync(projectName, tfvcPath, DateTime.UtcNow);

        LastResult = result;

        if (IsQuietMode == true)
        {
            return;
        }

        var formatter = new TfvcAssessmentReportFormatter();

        if (outputCsv == true)
        {
            WriteLine(formatter.FormatFindingsCsv(result));
        }
        else
        {
            WriteLine(string.Empty);
            WriteLine(formatter.FormatReport(result));
        }
    }

    private string GetTfvcPath(string projectName)
    {
        if (Arguments.ContainsKey(Constants.ArgumentNameTfvcFolder) == true &&
            Arguments[Constants.ArgumentNameTfvcFolder].HasValue == true)
        {
            var value = Arguments.GetStringValue(Constants.ArgumentNameTfvcFolder);

            if (string.IsNullOrWhiteSpace(value) == false)
            {
                return value;
            }
        }

        return $"$/{projectName}";
    }

    private int GetScanDepth()
    {
        if (Arguments.ContainsKey(Constants.ArgumentNameScanDepth) == true &&
            Arguments[Constants.ArgumentNameScanDepth].HasValue == true)
        {
            var value = Arguments.GetInt32Value(Constants.ArgumentNameScanDepth);

            if (value > 0)
            {
                return value;
            }
        }

        return TfvcFolderHeuristicScanner.DefaultMaxDepth;
    }

    private ITfvcApiClient CreateApiClient()
    {
        return new TfvcApiClient(async (requestUrl) =>
        {
            using var client = GetHttpClientInstanceForAzureDevOps();

            var response = await client.GetAsync(requestUrl);

            if (response.IsSuccessStatusCode == false)
            {
                return null;
            }

            return await response.Content.ReadAsStringAsync();
        });
    }
}
