using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.AgentCapabilities;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.Builds;

[Command(
    Category = Constants.Category_Builds,
    Name = Constants.CommandName_SetAgentCapabilities,
    Description =
        "Push a set of user-defined capabilities onto agents without going " +
        "through the UI. Target a whole pool with /pool, a single agent with " +
        "/agent, or every agent with /allpools. Supply the capabilities inline " +
        "with /capabilities:\"name=value;name2=value2\" and/or from a flat JSON " +
        "file with /input. Merges by default; use /replace to overwrite and " +
        "/preview to see the changes first.",
    IsAsync = true)]
public class SetAgentCapabilitiesCommand : AzureDevOpsCommandBase
{
    public SetAgentCapabilitiesCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);

        arguments.AddString(Constants.ArgumentNameCapabilities)
            .AsNotRequired()
            .WithDescription("Capabilities as name=value pairs separated by semicolons, e.g. \"VisualStudio=2022;SpecialSoftware=true\"");

        arguments.AddString(Constants.ArgumentNameInputFile)
            .AsNotRequired()
            .WithDescription("Path to a flat JSON file of name/value capabilities to apply");

        arguments.AddString(Constants.ArgumentNamePoolName)
            .AsNotRequired()
            .WithDescription("Apply to every agent in this pool");

        arguments.AddString(Constants.ArgumentNameAgentName)
            .AsNotRequired()
            .WithDescription("Apply to the agent with this name (optionally narrowed by /pool)");

        arguments.AddBoolean(Constants.ArgumentNameAllPools)
            .AllowEmptyValue()
            .WithDefaultValue(false)
            .AsNotRequired()
            .WithDescription("Apply to every agent in every pool");

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
        var pool = GetOptionalStringValue(Constants.ArgumentNamePoolName);
        var agent = GetOptionalStringValue(Constants.ArgumentNameAgentName);
        var allPools = Arguments.GetBooleanValue(Constants.ArgumentNameAllPools);
        var replace = Arguments.GetBooleanValue(Constants.ArgumentNameReplace);
        var previewOnly = Arguments.GetBooleanValue(Constants.ArgumentNamePreviewOnly);

        var hasPool = string.IsNullOrWhiteSpace(pool) == false;
        var hasAgent = string.IsNullOrWhiteSpace(agent) == false;

        ValidateTargeting(hasPool, hasAgent, allPools);

        var desired = ReadDesiredCapabilities();

        if (desired.Count == 0)
        {
            throw new KnownException(
                $"No capabilities to apply. Supply /{Constants.ArgumentNameCapabilities} " +
                $"and/or /{Constants.ArgumentNameInputFile}.");
        }

        var service = CreateAgentCapabilityService();

        // Narrow the inventory to the pool when one is named; otherwise the agent
        // could be in any pool, so everything is read and filtered below.
        var inventory = await service.GetInventoryAsync(hasPool ? pool : null);

        var targets = inventory.AsEnumerable();

        if (hasAgent == true)
        {
            targets = targets.Where(x =>
                string.Equals(x.AgentName, agent, StringComparison.OrdinalIgnoreCase));
        }

        var targetList = targets.ToList();

        if (targetList.Count == 0)
        {
            WriteLine();
            WriteLine("No agents matched the target. Nothing to do.");
            return;
        }

        WriteLine();
        WriteLine($"Applying {desired.Count} capabilit{(desired.Count == 1 ? "y" : "ies")} to {targetList.Count} agent(s).");
        WriteLine(replace
            ? "Mode: REPLACE (each agent's user capabilities will be overwritten)."
            : "Mode: MERGE (capabilities are layered onto what each agent already has).");

        var plan = service.PlanSet(targetList, desired, replace);

        await AgentCapabilityCommandHelper.ApplyPlanAsync(
            service, plan, previewOnly, WriteLine);
    }

    private static void ValidateTargeting(bool hasPool, bool hasAgent, bool allPools)
    {
        if (allPools == true && (hasPool == true || hasAgent == true))
        {
            throw new KnownException(
                $"/{Constants.ArgumentNameAllPools} cannot be combined with " +
                $"/{Constants.ArgumentNamePoolName} or /{Constants.ArgumentNameAgentName}.");
        }

        if (allPools == false && hasPool == false && hasAgent == false)
        {
            throw new KnownException(
                $"Specify a target: /{Constants.ArgumentNamePoolName}, " +
                $"/{Constants.ArgumentNameAgentName}, or /{Constants.ArgumentNameAllPools}.");
        }
    }

    /// <summary>
    /// The capabilities to apply, taken from the flat JSON file first and then
    /// the inline value, so an inline pair overrides the same key in the file.
    /// </summary>
    private Dictionary<string, string> ReadDesiredCapabilities()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var inputPath = GetOptionalStringValue(Constants.ArgumentNameInputFile);

        if (string.IsNullOrWhiteSpace(inputPath) == false)
        {
            if (File.Exists(inputPath) == false)
            {
                throw new KnownException($"Capabilities file '{inputPath}' does not exist.");
            }

            Dictionary<string, string>? fromFile;

            try
            {
                fromFile = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    File.ReadAllText(inputPath));
            }
            catch (JsonException ex)
            {
                throw new KnownException(
                    $"Capabilities file '{inputPath}' is not a flat JSON object of name/value pairs: {ex.Message}");
            }

            if (fromFile != null)
            {
                foreach (var pair in fromFile)
                {
                    result[pair.Key] = pair.Value;
                }
            }
        }

        var inline = CapabilityStringParser.Parse(
            GetOptionalStringValue(Constants.ArgumentNameCapabilities));

        foreach (var pair in inline)
        {
            result[pair.Key] = pair.Value;
        }

        return result;
    }
}
