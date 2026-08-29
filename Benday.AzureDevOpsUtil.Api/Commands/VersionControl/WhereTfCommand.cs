using Benday.AzureDevOpsUtil.Api.TfCommandLine;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.VersionControl;

[Command(
    Category = Constants.Category_VersionControl,
    Name = Constants.CommandName_WhereTf,
    Description =
        "Finds the tf command line client, which ships inside Visual Studio and is rarely on the PATH.")]
public class WhereTfCommand : Command
{
    public WhereTfCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {
    }

    public List<TfLocation> LastResult { get; private set; } = new();

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        arguments.AddBoolean(Constants.ArgumentNameQuietMode)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Print only the path of the first copy that was found");

        return arguments;
    }

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        var quiet = Arguments.GetBooleanValue(Constants.ArgumentNameQuietMode);

        var locator = new TfExecutableLocator(new FileSystemProbe());

        var locations = locator.Find();

        LastResult = locations;

        if (locations.Count == 0)
        {
            if (quiet == false)
            {
                WriteNotFound();
            }

            return Task.CompletedTask;
        }

        if (quiet == true)
        {
            // One bare path, so a script can capture it.
            WriteLine(locations[0].Path);

            return Task.CompletedTask;
        }

        WriteLine(
            locations.Count == 1 ?
                "Found 1 copy of tf:" :
                $"Found {locations.Count} copies of tf:");

        WriteLine(string.Empty);

        foreach (var location in locations)
        {
            WriteLine(location.Path);
            WriteLine(
                $"    {location.Source}{(location.IsOnPath == true ? ", already on the PATH" : string.Empty)}");
        }

        WriteOnPathAdvice(locations);

        return Task.CompletedTask;
    }

    private void WriteOnPathAdvice(List<TfLocation> locations)
    {
        if (locations.Any(x => x.IsOnPath == true) == true)
        {
            return;
        }

        var best = locations[0];

        var directory = Path.GetDirectoryName(best.Path);

        if (string.IsNullOrWhiteSpace(directory) == true)
        {
            return;
        }

        WriteLine(string.Empty);
        WriteLine("None of these are on the PATH. To add the first one for this session:");
        WriteLine(string.Empty);
        WriteLine($"    PowerShell:  $env:PATH += \";{directory}\"");
        WriteLine($"    cmd:         set PATH=%PATH%;{directory}");
    }

    private void WriteNotFound()
    {
        WriteLine("No copy of tf was found.");
        WriteLine(string.Empty);
        WriteLine("tf is not distributed on its own and there is no winget package for it.");
        WriteLine("It arrives with:");
        WriteLine("  - Visual Studio, when the Team Explorer component is installed");
        WriteLine("  - an Azure DevOps Server or Team Foundation Server install");
        WriteLine(
            "  - the Team Explorer Everywhere command line client, which runs on macOS and Linux");
        WriteLine(string.Empty);
        WriteLine("Searched the PATH, the Visual Studio install folders under Program Files,");
        WriteLine("and the Azure DevOps Server install folders.");
    }
}
