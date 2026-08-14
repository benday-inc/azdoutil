using System.Diagnostics;

namespace Benday.AzureDevOpsUtil.Api.TfCommandLine;

/// <summary>
/// Runs the tf command line client.  Behind an interface so everything that
/// reads its output can be tested without one being installed.
/// </summary>
public interface ITfCommandRunner
{
    /// <summary>
    /// Runs tf in a directory and returns what it printed, or null when it
    /// could not be run at all.
    /// </summary>
    string? Run(string executablePath, string workingDirectory, params string[] arguments);
}

public class TfCommandRunner : ITfCommandRunner
{
    public string? Run(string executablePath, string workingDirectory, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            using var process = Process.Start(startInfo);

            if (process == null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();

            process.WaitForExit();

            // tf sets a non-zero exit code in situations where it still printed
            // what was asked for, so the output decides rather than the code.
            return string.IsNullOrWhiteSpace(output) == true ? null : output;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
