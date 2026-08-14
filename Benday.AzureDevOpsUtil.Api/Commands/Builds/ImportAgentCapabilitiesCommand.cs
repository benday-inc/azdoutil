using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.AgentCapabilities;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.Builds;

[Command(
    Category = Constants.Category_Builds,
    Name = Constants.CommandName_ImportAgentCapabilities,
    Description =
        "Reapply the user-defined capabilities from an exportagentcapabilities " +
        "file onto the agents of the current server, matching agents by name. " +
        "By default the imported capabilities are merged onto whatever each agent " +
        "already has; use /replace to overwrite. Use /preview to see what would " +
        "change without writing anything.",
    IsAsync = true)]
public class ImportAgentCapabilitiesCommand : AzureDevOpsCommandBase
{
    public ImportAgentCapabilitiesCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);

        arguments.AddFile(Constants.ArgumentNameInputFile)
            .WithDescription("Path to the JSON file produced by exportagentcapabilities")
            .MustExist()
            .AsRequired()
            .FromPositionalArgument(1);

        arguments.AddBoolean(Constants.ArgumentNameReplace)
            .AllowEmptyValue()
            .WithDefaultValue(false)
            .AsNotRequired()
            .WithDescription("Overwrite each agent's user capabilities instead of merging");

        arguments.AddBoolean(Constants.ArgumentNamePreviewOnly)
            .AllowEmptyValue()
            .WithDefaultValue(false)
            .AsNotRequired()
            .WithDescription("Preview the changes without writing anything");

        return arguments;
    }

    protected override async Task OnExecute()
    {
        var inputPath = Arguments.GetPathToFile(Constants.ArgumentNameInputFile);
        var replace = Arguments.GetBooleanValue(Constants.ArgumentNameReplace);
        var previewOnly = Arguments.GetBooleanValue(Constants.ArgumentNamePreviewOnly);

        AgentCapabilityExport? export;

        try
        {
            export = JsonSerializer.Deserialize<AgentCapabilityExport>(
                File.ReadAllText(inputPath), JsonUtilities.DefaultOptions);
        }
        catch (JsonException ex)
        {
            throw new KnownException($"Could not read the capabilities file '{inputPath}': {ex.Message}");
        }

        if (export == null || export.Agents.Count == 0)
        {
            throw new KnownException(
                $"The capabilities file '{inputPath}' has no agents to import.");
        }

        var service = CreateAgentCapabilityService();

        var inventory = await service.GetInventoryAsync();

        var plan = service.PlanImport(inventory, export, replace);

        WriteLine();
        WriteLine(
            $"Read {export.Agents.Count} agent(s) from the file. Matched {plan.Matched.Count} " +
            $"to agents on this server; {plan.Unmatched.Count} unmatched.");

        WriteLine();
        WriteLine(replace
            ? "Mode: REPLACE (each matched agent's user capabilities will be overwritten)."
            : "Mode: MERGE (imported capabilities are layered onto what each agent already has).");

        await AgentCapabilityCommandHelper.ApplyPlanAsync(
            service, plan.Matched, previewOnly, WriteLine);

        if (plan.Unmatched.Count > 0)
        {
            WriteLine();
            WriteLine("Unmatched agents from the file (no agent with that name on this server):");

            foreach (var record in plan.Unmatched)
            {
                WriteLine($"   - {record.AgentName} (from pool '{record.PoolName}')");
            }
        }
    }
}
