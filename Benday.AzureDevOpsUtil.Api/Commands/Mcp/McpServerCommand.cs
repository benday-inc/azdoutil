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
            .AddMcpServer(options =>
            {
                // Sent to the client at startup and surfaced to the model to
                // help it route delivery/flow-metrics questions to these tools.
                options.ServerInstructions =
                    "This server answers questions about Azure DevOps delivery using flow metrics. " +
                    "Use its tools when someone asks how long work usually takes (delivery window / " +
                    "cycle time), how much the team gets done (throughput / velocity), when a number " +
                    "of items will be finished or how much fits in a timeframe (Monte Carlo forecasts), " +
                    "or what is stuck or aging. It also exposes read-only context tools (projects, " +
                    "teams, work item types/states, queries, repositories). Call list_configurations " +
                    "first if you are unsure which Azure DevOps connection to use or if a tool reports " +
                    "a missing configuration. If the user asks for an Azure DevOps task that no tool " +
                    "here covers, call discover_cli_commands to find the matching azdoutil command-line " +
                    "command and suggest it instead of saying it can't be done.";
            })
            .WithStdioServerTransport()
            .WithTools<DeliveryIntelligenceTools>()
            .WithTools<ConfigurationTools>()
            .WithTools<AzureDevOpsContextTools>()
            .WithTools<CliDiscoveryTools>();

        var host = builder.Build();

        await host.RunAsync();
    }
}
