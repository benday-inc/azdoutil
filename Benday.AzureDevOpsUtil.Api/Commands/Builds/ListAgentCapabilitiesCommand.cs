using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.AgentCapabilities;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.Builds;

[Command(
    Category = Constants.Category_Builds,
    Name = Constants.CommandName_ListAgentCapabilities,
    Description =
        "List the build agents across all agent pools and the user-defined " +
        "capabilities each one has. Use /customonly to show only the agents " +
        "that have custom capabilities.",
    IsAsync = true)]
public class ListAgentCapabilitiesCommand : AzureDevOpsCommandBase
{
    public IReadOnlyList<AgentCapabilityRecord>? LastResult { get; private set; }

    public ListAgentCapabilitiesCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);

        arguments.AddString(Constants.ArgumentNamePoolName)
            .AsNotRequired()
            .WithDescription("Only look at this agent pool");

        arguments.AddBoolean(Constants.ArgumentNameCustomOnly)
            .AllowEmptyValue()
            .WithDefaultValue(false)
            .AsNotRequired()
            .WithDescription("Only show agents that have user-defined capabilities");

        arguments.AddBoolean(Constants.CommandArgumentNameToJson)
            .AllowEmptyValue()
            .WithDefaultValue(false)
            .AsNotRequired()
            .WithDescription("Output as JSON");

        return arguments;
    }

    protected override async Task OnExecute()
    {
        var poolFilter = GetOptionalStringValue(Constants.ArgumentNamePoolName);
        var customOnly = Arguments.GetBooleanValue(Constants.ArgumentNameCustomOnly);
        var toJson = Arguments.GetBooleanValue(Constants.CommandArgumentNameToJson);

        var service = CreateAgentCapabilityService();

        var inventory = await service.GetInventoryAsync(
            string.IsNullOrWhiteSpace(poolFilter) ? null : poolFilter);

        if (customOnly == true)
        {
            inventory = inventory.Where(x => x.UserCapabilities.Count > 0).ToList();
        }

        LastResult = inventory;

        if (IsQuietMode == true)
        {
            return;
        }

        if (toJson == true)
        {
            WriteLine(JsonSerializer.Serialize(inventory, new JsonSerializerOptions
            {
                WriteIndented = true
            }));

            return;
        }

        WriteReport(inventory, customOnly);
    }

    private void WriteReport(IReadOnlyList<AgentCapabilityRecord> inventory, bool customOnly)
    {
        WriteLine();

        var descriptor = customOnly ? "agent(s) with custom capabilities" : "agent(s)";
        WriteLine($"Found {inventory.Count} {descriptor}.");

        foreach (var group in inventory.GroupBy(x => x.PoolName).OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            WriteLine();
            WriteLine($"** Pool: {group.Key}");

            foreach (var agent in group.OrderBy(x => x.AgentName, StringComparer.OrdinalIgnoreCase))
            {
                var enabledMarker = agent.Enabled ? "enabled" : "disabled";
                var capCount = agent.UserCapabilities.Count;

                WriteLine($"   - {agent.AgentName} (agent id: {agent.AgentId}, {enabledMarker}, {agent.Status}) " +
                    $"- {capCount} user capabilit{(capCount == 1 ? "y" : "ies")}");

                foreach (var cap in agent.UserCapabilities.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                {
                    WriteLine($"       {cap.Key} = {cap.Value}");
                }
            }
        }
    }
}
