using Benday.AzureDevOpsUtil.Api.TfCommandLine;
using Benday.AzureDevOpsUtil.Api.TfvcAssessment;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.VersionControl;

[Command(
    Category = Constants.Category_VersionControl,
    Name = Constants.CommandName_AssessTfvcMigration,
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
            .AsNotRequired()
            .WithDescription(
                "Team project name. Read from the TFVC workspace holding the current " +
                "directory when it is not supplied.");

        arguments.AddString(Constants.ArgumentNameTfvcFolder)
            .AsNotRequired()
            .WithDescription(
                "TFVC path to assess. Defaults to the server path of the current directory " +
                "when it is inside a workspace, and to $/<teamproject> otherwise.");

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

    protected override async Task OnExecute(CancellationToken cancellationToken)
    {
        var projectName = GetOptionalStringValue(Constants.ArgumentNameTeamProjectName);
        var tfvcPath = GetOptionalStringValue(Constants.ArgumentNameTfvcFolder);

        if (projectName.Length == 0 || tfvcPath.Length == 0)
        {
            var location = ReadCurrentWorkspaceLocation();

            if (projectName.Length == 0)
            {
                projectName = location.TeamProjectName;
            }

            if (tfvcPath.Length == 0)
            {
                tfvcPath = location.ServerPath;
            }

            UseConfigurationForCollection(location);

            if (IsQuietMode == false)
            {
                WriteLine(
                    $"Using the TFVC workspace holding this directory: project " +
                    $"'{projectName}', path '{tfvcPath}' at {location.CollectionUrl}");
                WriteLine(string.Empty);
            }
        }

        if (tfvcPath.StartsWith("$/", StringComparison.Ordinal) == false)
        {
            throw new KnownException(
                $"The value for --{Constants.ArgumentNameTfvcFolder} should start with '$/'.  " +
                $"For example: $/{projectName}/Main");
        }

        var outputCsv = Arguments.GetBooleanValue(Constants.ArgumentNameOutputCsv);

        // Without this, a mistyped path reads as a real result: the report says
        // there are no branches and no history, which looks like a finding
        // rather than a typo.
        await ValidateTfvcPathExists(projectName, tfvcPath);

        var analyzer = new TfvcAssessmentAnalyzer(
            new TfvcApiClient(GetJsonAsync),
            new BuildDefinitionApiClient(GetJsonAsync))
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

    /// <summary>
    /// Works out where the current directory sits in TFVC.  Each way this can
    /// fail says something different, so each gets its own message.
    /// </summary>
    private TfvcLocationInfo ReadCurrentWorkspaceLocation()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        var tf = new TfExecutableLocator(new FileSystemProbe()).Find().FirstOrDefault();

        if (tf == null)
        {
            throw new KnownException(
                "The tf command line client is needed to work out which TFVC path this " +
                $"directory holds, and no copy was found. Run {Constants.CommandName_WhereTf} " +
                $"for where it was looked for, or supply " +
                $"--{Constants.ArgumentNameTeamProjectName} and " +
                $"--{Constants.ArgumentNameTfvcFolder}.");
        }

        var output = new TfCommandRunner().Run(tf.Path, currentDirectory, "workfold");

        var workspace = TfWorkfoldParser.Parse(output);

        if (workspace == null)
        {
            throw new KnownException(
                $"'{tf.Path} workfold' did not report a workspace for '{currentDirectory}'. " +
                $"Supply --{Constants.ArgumentNameTeamProjectName} and " +
                $"--{Constants.ArgumentNameTfvcFolder}.");
        }

        var location = TfWorkspaceResolver.Resolve(workspace, currentDirectory);

        if (location == null)
        {
            var mapped = string.Join(
                ", ", workspace.Mappings.Where(x => x.IsCloaked == false).Select(x => x.LocalPath));

            throw new KnownException(
                $"'{currentDirectory}' is not inside any folder mapped by workspace " +
                $"'{workspace.WorkspaceName}'. That workspace maps: {mapped}. Supply " +
                $"--{Constants.ArgumentNameTeamProjectName} and " +
                $"--{Constants.ArgumentNameTfvcFolder}.");
        }

        if (string.IsNullOrWhiteSpace(location.TeamProjectName) == true)
        {
            throw new KnownException(
                $"The current directory maps to '{location.ServerPath}', which does not name a " +
                $"team project. Supply --{Constants.ArgumentNameTeamProjectName} and " +
                $"--{Constants.ArgumentNameTfvcFolder}.");
        }

        return location;
    }

    /// <summary>
    /// Picks the stored configuration for the collection the workspace belongs
    /// to.  An explicit /config wins.
    /// </summary>
    private void UseConfigurationForCollection(TfvcLocationInfo location)
    {
        if (Arguments.HasValue(Constants.ArgumentNameConfigurationName) == true)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(location.CollectionUrl) == true)
        {
            return;
        }

        var configurations = AzureDevOpsConfigurationManager.Instance.GetAll();

        var match = configurations.FirstOrDefault(x =>
            string.Equals(
                (x.CollectionUrl ?? string.Empty).TrimEnd('/'),
                location.CollectionUrl.TrimEnd('/'),
                StringComparison.OrdinalIgnoreCase));

        if (match == null)
        {
            var known = configurations.Length == 0 ?
                "There are no configurations." :
                "Configurations: " + string.Join(
                    ", ", configurations.Select(x => $"{x.Name} ({x.CollectionUrl})")) + ".";

            throw new KnownException(
                $"This workspace belongs to {location.CollectionUrl}, and no azdoutil " +
                $"configuration uses that url. {known} Add one with " +
                $"{Constants.CommandArgumentNameAddUpdateConfig}, or name a configuration with " +
                $"--{Constants.ArgumentNameConfigurationName}.");
        }

        Configuration = match;
    }

    private int GetScanDepth()
    {
        if (Arguments.HasValue(Constants.ArgumentNameScanDepth) == true)
        {
            var value = Arguments.GetInt32Value(Constants.ArgumentNameScanDepth);

            if (value > 0)
            {
                return value;
            }
        }

        return TfvcFolderHeuristicScanner.DefaultMaxDepth;
    }

    /// <summary>
    /// Confirms the path can be read before any analysis runs, and reports what
    /// the server said when it cannot.
    /// </summary>
    private async Task ValidateTfvcPathExists(string projectName, string tfvcPath)
    {
        using var client = GetHttpClientInstanceForAzureDevOps();

        var requestUrl =
            $"{Uri.EscapeDataString(projectName)}/_apis/tfvc/items" +
            $"?scopePath={Uri.EscapeDataString(tfvcPath)}" +
            $"&recursionLevel=None" +
            $"&api-version={TfvcApiClient.ApiVersion}";

        var response = await client.GetAsync(requestUrl);

        if (response.IsSuccessStatusCode == true)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();

        var message = AzureDevOpsErrorMessageReader.GetMessageOrDefault(
            body, $"{(int)response.StatusCode} {response.ReasonPhrase}");

        var hint = string.Empty;

        var suggestion = TfvcPath.SuggestProjectRootedPath(tfvcPath, projectName);

        if (suggestion != null)
        {
            hint =
                " TFVC paths start with the team project name, which the web UI shows in its " +
                $"own selector rather than in the folder breadcrumb. Did you mean '{suggestion}'?";
        }

        throw new KnownException(
            $"Could not read TFVC path '{tfvcPath}' in team project '{projectName}'. " +
            $"{message}{hint}");
    }

    /// <summary>
    /// Issues an authenticated GET and returns the body, or null when the call
    /// did not succeed.  Both API clients are built on this.
    /// </summary>
    private async Task<string?> GetJsonAsync(string requestUrl)
    {
        using var client = GetHttpClientInstanceForAzureDevOps();

        var response = await client.GetAsync(requestUrl);

        if (response.IsSuccessStatusCode == false)
        {
            return null;
        }

        return await response.Content.ReadAsStringAsync();
    }
}
