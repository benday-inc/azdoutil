using System.Text.Json;

namespace Benday.AzureDevOpsUtil.Api.TaskGroups;

public static class BuildDefinitionTaskGroupScanner
{
    public const string MetaTaskDefinitionType = "metaTask";

    public static List<TaskGroupReference> FindReferences(string buildDefinitionJson)
    {
        if (string.IsNullOrWhiteSpace(buildDefinitionJson))
        {
            return new List<TaskGroupReference>();
        }

        using var document = JsonDocument.Parse(buildDefinitionJson);
        return FindReferences(document.RootElement);
    }

    public static List<TaskGroupReference> FindReferences(JsonElement root)
    {
        var results = new List<TaskGroupReference>();

        if (root.ValueKind != JsonValueKind.Object)
        {
            return results;
        }

        if (!root.TryGetProperty("process", out var process) ||
            process.ValueKind != JsonValueKind.Object)
        {
            return results;
        }

        if (!process.TryGetProperty("phases", out var phases) ||
            phases.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        var phaseIndex = 0;
        foreach (var phase in phases.EnumerateArray())
        {
            var phaseName = ReadString(phase, "name", ReadString(phase, "refName"));

            if (phase.TryGetProperty("steps", out var steps) &&
                steps.ValueKind == JsonValueKind.Array)
            {
                var stepIndex = 0;
                foreach (var step in steps.EnumerateArray())
                {
                    var reference = TryReadMetaTaskReference(step, phaseIndex, phaseName, stepIndex);
                    if (reference != null)
                    {
                        results.Add(reference);
                    }
                    stepIndex++;
                }
            }

            phaseIndex++;
        }

        return results;
    }

    private static TaskGroupReference? TryReadMetaTaskReference(
        JsonElement step, int phaseIndex, string phaseName, int stepIndex)
    {
        if (step.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!step.TryGetProperty("task", out var task) ||
            task.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var definitionType = ReadString(task, "definitionType");
        if (!string.Equals(definitionType, MetaTaskDefinitionType, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new TaskGroupReference
        {
            TaskGroupId = ReadString(task, "id"),
            VersionSpec = ReadString(task, "versionSpec"),
            PhaseIndex = phaseIndex,
            PhaseName = phaseName,
            StepIndex = stepIndex,
            StepDisplayName = ReadString(step, "displayName"),
            Enabled = ReadBool(step, "enabled", defaultValue: true)
        };
    }

    private static string ReadString(JsonElement element, string propertyName, string defaultValue = "")
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return defaultValue;
        }

        if (!element.TryGetProperty(propertyName, out var prop))
        {
            return defaultValue;
        }

        return prop.ValueKind == JsonValueKind.String ? (prop.GetString() ?? defaultValue) : defaultValue;
    }

    private static bool ReadBool(JsonElement element, string propertyName, bool defaultValue)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return defaultValue;
        }

        if (!element.TryGetProperty(propertyName, out var prop))
        {
            return defaultValue;
        }

        return prop.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => defaultValue
        };
    }
}
