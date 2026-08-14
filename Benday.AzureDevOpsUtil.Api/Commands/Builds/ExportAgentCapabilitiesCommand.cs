using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.AgentCapabilities;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.Builds;

[Command(
    Category = Constants.Category_Builds,
    Name = Constants.CommandName_ExportAgentCapabilities,
    Description =
        "Script out the user-defined capabilities of the build agents to a JSON " +
        "file so they can be reapplied to a new server with importagentcapabilities. " +
        "Only agents that have custom capabilities are written.",
    IsAsync = true)]
public class ExportAgentCapabilitiesCommand : AzureDevOpsCommandBase
{
    public AgentCapabilityExport? LastResult { get; private set; }

    public ExportAgentCapabilitiesCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);

        arguments.AddString(Constants.ArgumentNamePoolName)
            .AsNotRequired()
            .WithDescription("Only export agents in this agent pool");

        arguments.AddString(Constants.ArgumentNameOutputFile)
            .AsNotRequired()
            .WithDescription("Path to write the JSON file to. If omitted, the JSON is written to the console.");

        return arguments;
    }

    protected override async Task OnExecute()
    {
        var poolFilter = GetOptionalStringValue(Constants.ArgumentNamePoolName);
        var outputPath = GetOptionalStringValue(Constants.ArgumentNameOutputFile);

        var service = CreateAgentCapabilityService();

        var inventory = await service.GetInventoryAsync(
            string.IsNullOrWhiteSpace(poolFilter) ? null : poolFilter);

        var export = AgentCapabilityService.BuildExport(Configuration.CollectionUrl, inventory);

        LastResult = export;

        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var capabilityCount = export.Agents.Sum(x => x.UserCapabilities.Count);

        if (string.IsNullOrWhiteSpace(outputPath) == true)
        {
            if (IsQuietMode == false)
            {
                WriteLine(json);
            }
        }
        else
        {
            File.WriteAllText(outputPath, json);

            if (IsQuietMode == false)
            {
                WriteLine();
                WriteLine(
                    $"Wrote {export.Agents.Count} agent(s) with {capabilityCount} user " +
                    $"capabilit{(capabilityCount == 1 ? "y" : "ies")} to '{outputPath}'.");
            }
        }
    }
}
