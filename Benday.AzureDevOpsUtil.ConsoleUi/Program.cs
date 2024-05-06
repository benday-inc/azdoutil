using System.Diagnostics;
using System.Reflection;
using System.Text;

using Benday.AzureDevOpsUtil.Api;
using Benday.CommandsFramework;

class Program
{
    static void Main(string[] args)
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

        var program = new DefaultProgram(options, assembly);

        program.Run(args);
    }
}