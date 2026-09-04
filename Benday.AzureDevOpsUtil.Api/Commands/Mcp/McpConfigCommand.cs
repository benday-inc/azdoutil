using System.Diagnostics;
using System.Runtime.InteropServices;

using Benday.AzureDevOpsUtil.Api.McpTools;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.Mcp;

[Command(
    Category = Constants.Category_Mcp,
    Name = Constants.CommandArgumentNameMcpConfig,
    Description =
        "Show or manage the MCP server registration for an AI client. With no options it prints " +
        "ready-to-paste configuration; with --install or --uninstall it registers or removes the " +
        "server at user scope (per-machine) for Claude Code or VS Code.")]
public class McpConfigCommand : Command
{
    private const string ClientClaudeCode = "claude-code";
    private const string ClientVsCode = "vscode";
    private const string ClientPrint = "print";

    public McpConfigCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        arguments.AddBoolean(Constants.ArgumentNameMcpInstall)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Register the MCP server with a client at user (per-machine) scope");

        arguments.AddBoolean(Constants.ArgumentNameMcpUninstall)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Remove the MCP server registration from a client");

        arguments.AddString(Constants.ArgumentNameMcpClient)
            .AsNotRequired()
            .WithDescription("Target client: claude-code (default) or vscode");

        arguments.AddString(Constants.ArgumentNameConfigurationName)
            .AsNotRequired()
            .WithDescription("azdoutil configuration the server should use by default (sets AZDO_CONFIG_NAME)");

        return arguments;
    }

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        var install = IsFlagSet(Constants.ArgumentNameMcpInstall);
        var uninstall = IsFlagSet(Constants.ArgumentNameMcpUninstall);

        if (install && uninstall)
        {
            throw new KnownException("Specify either --install or --uninstall, not both.");
        }

        var configName = GetOptionalConfigName();
        var client = GetClient(install || uninstall);

        if (install)
        {
            Install(client, configName);
        }
        else if (uninstall)
        {
            Uninstall(client);
        }
        else
        {
            WriteLine(McpClientSetup.PrintableInstructions(configName));
        }

        return Task.CompletedTask;
    }

    private bool IsFlagSet(string name)
    {
        return Arguments.HasValue(name);
    }

    private string? GetOptionalConfigName()
    {
        if (Arguments.HasValue(Constants.ArgumentNameConfigurationName) == true)
        {
            var value = Arguments.GetStringValue(Constants.ArgumentNameConfigurationName);

            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }

    private string GetClient(bool isInstallOrUninstall)
    {
        if (Arguments.HasValue(Constants.ArgumentNameMcpClient) == true)
        {
            var value = Arguments.GetStringValue(Constants.ArgumentNameMcpClient).Trim().ToLowerInvariant();

            if (value == ClientClaudeCode || value == ClientVsCode || value == ClientPrint)
            {
                return value;
            }

            throw new KnownException(
                $"Unknown client '{value}'. Supported clients: {ClientClaudeCode}, {ClientVsCode}.");
        }

        // Install/uninstall default to Claude Code; with no action we just print.
        return isInstallOrUninstall ? ClientClaudeCode : ClientPrint;
    }

    private void Install(string client, string? configName)
    {
        switch (client)
        {
            case ClientClaudeCode:
                RunClientCommand(
                    "claude", McpClientSetup.ClaudeAddArguments(configName),
                    "Registering the azdoutil MCP server with Claude Code (user scope)...",
                    "claude", configName);
                break;

            case ClientVsCode:
                RunClientCommand(
                    "code", new[] { "--add-mcp", McpClientSetup.VsCodeAddMcpJson(configName) },
                    "Registering the azdoutil MCP server in your VS Code user profile...",
                    "code", configName);
                break;

            default:
                // client == print
                WriteLine(McpClientSetup.PrintableInstructions(configName));
                break;
        }
    }

    private void Uninstall(string client)
    {
        switch (client)
        {
            case ClientClaudeCode:
                RunClientCommand(
                    "claude", McpClientSetup.ClaudeRemoveArguments(),
                    "Removing the azdoutil MCP server from Claude Code...",
                    "claude", null);
                break;

            case ClientVsCode:
                WriteLine("To remove the azdoutil MCP server from VS Code, run the " +
                          "'MCP: Open User Configuration' command and delete the \"azdoutil\" entry " +
                          "from the \"servers\" section.");
                break;

            default:
                WriteLine("Nothing to uninstall for the 'print' client. " +
                          "Use --client claude-code or --client vscode.");
                break;
        }
    }

    private void RunClientCommand(
        string fileName, IReadOnlyList<string> arguments,
        string startMessage, string clientDisplayName, string? configName)
    {
        WriteLine(startMessage);

        try
        {
            var (exitCode, output) = RunProcess(fileName, arguments);

            if (string.IsNullOrWhiteSpace(output) == false)
            {
                WriteLine(output.Trim());
            }

            if (exitCode == 0)
            {
                WriteLine("Done.");
            }
            else
            {
                WriteLine($"'{clientDisplayName}' exited with code {exitCode}. See the output above.");
                WriteManualFallback(configName);
            }
        }
        catch (Exception ex)
        {
            WriteLine($"Could not run '{clientDisplayName}': {ex.Message}");
            WriteLine($"Make sure the '{clientDisplayName}' command-line tool is installed and on your PATH.");
            WriteManualFallback(configName);
        }
    }

    private void WriteManualFallback(string? configName)
    {
        WriteLine(string.Empty);
        WriteLine("You can configure the server manually instead:");
        WriteLine(McpClientSetup.PrintableInstructions(configName));
    }

    private static (int ExitCode, string Output) RunProcess(
        string fileName, IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // On Windows, client CLIs are often .cmd shims that CreateProcess
            // cannot launch directly, so route through the command interpreter.
            startInfo.FileName = "cmd.exe";
            startInfo.ArgumentList.Add("/c");
            startInfo.ArgumentList.Add(fileName);
        }
        else
        {
            startInfo.FileName = fileName;
        }

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start '{fileName}'.");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();

        process.WaitForExit();

        var combined = string.Join(
            Environment.NewLine,
            new[] { stdout, stderr }.Where(x => string.IsNullOrWhiteSpace(x) == false));

        return (process.ExitCode, combined);
    }
}
