using Benday.AzureDevOpsUtil.Api.FlowMetrics;
using Benday.AzureDevOpsUtil.Api.McpTools;
using Benday.CommandsFramework;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Benday.AzureDevOpsUtil.Api.Commands.Mcp;

[Command(
    Category = Constants.Category_Mcp,
    Name = Constants.CommandArgumentNameMcpServer,
    Description =
        "Start a Model Context Protocol (MCP) server over stdio that exposes the flow metrics " +
        "tools to an AI assistant. The process stays alive until the MCP client disconnects.",
    IsAsync = true)]
public class McpServerCommand : AsynchronousCommand
{
    public McpServerCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        // The MCP server takes no command-line arguments. Connection details
        // come from the stored azdoutil configuration; the active configuration
        // name is supplied per tool call or via the AZDO_CONFIG_NAME
        // environment variable.
        return new ArgumentCollection();
    }

    protected override async Task OnExecute()
    {
        // IMPORTANT: stdio is the MCP JSON-RPC transport, so nothing may be
        // written to stdout here. All diagnostic logging is routed to stderr.
        var builder = Host.CreateApplicationBuilder();

        builder.Logging.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        builder.Services.AddSingleton<FlowMetricsService>();

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<DeliveryIntelligenceTools>();

        var host = builder.Build();

        await host.RunAsync();
    }
}
