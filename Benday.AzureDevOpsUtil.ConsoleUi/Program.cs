using System.Diagnostics;
using System.Reflection;
using System.Text;

using Benday.AzureDevOpsUtil.Api;
using Benday.CommandsFramework;
using Benday.CommandsFramework.Tui;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var assembly = typeof(StringUtility).Assembly;

        var versionInfo =
            FileVersionInfo.GetVersionInfo(
                Assembly.GetExecutingAssembly().Location);

        var options = new DefaultProgramOptions();

        options.Version = $"v{versionInfo.FileVersion}";
        options.ApplicationName = "Azure DevOps Utilities";
        options.Website = "https://www.benday.com";
        options.DisplayUsageOptions.ShowCategories = true;
        options.StrictArgumentValidation = false;

        // gives the tool the 'tui' keyword. This is the equivalent of the CommandsApp
        // builder's .WithTui() for a program that configures its options directly.
        options.TuiHost = new SpectreTuiHost();

        var program = new DefaultProgram(options, assembly);

        return await program.RunAsync(args);
    }
}