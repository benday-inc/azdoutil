using System.Text.Json.Nodes;

using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.TaskGroups;

public class BuildDefinitionInliner
{
    public const string InlinedDisplayNamePrefix = "[INLINED] ";

    private readonly Dictionary<string, JsonNode> _taskGroupsById;

    public BuildDefinitionInliner(IDictionary<string, JsonNode> taskGroupsById)
    {
        if (taskGroupsById == null)
        {
            throw new ArgumentNullException(nameof(taskGroupsById));
        }

        _taskGroupsById = new Dictionary<string, JsonNode>(taskGroupsById, StringComparer.OrdinalIgnoreCase);
    }

    public InlineResult Inline(JsonNode buildDefinition, string? taskGroupIdFilter = null)
    {
        if (buildDefinition == null)
        {
            throw new ArgumentNullException(nameof(buildDefinition));
        }

        ValidateNoNestedTaskGroups(taskGroupIdFilter);

        var result = new InlineResult();

        if (buildDefinition["process"] is not JsonObject process)
        {
            return result;
        }

        if (process["phases"] is not JsonArray phases)
        {
            return result;
        }

        foreach (var phase in phases)
        {
            if (phase is JsonObject phaseObj)
            {
                InlinePhase(phaseObj, taskGroupIdFilter, result);
            }
        }

        return result;
    }

    private void ValidateNoNestedTaskGroups(string? taskGroupIdFilter)
    {
        foreach (var pair in _taskGroupsById)
        {
            if (taskGroupIdFilter != null &&
                !string.Equals(pair.Key, taskGroupIdFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var taskGroup = pair.Value;
            if (taskGroup["tasks"] is not JsonArray tasks)
            {
                continue;
            }

            foreach (var step in tasks)
            {
                var defType = step?["task"]?["definitionType"]?.GetValue<string>();
                if (string.Equals(defType, BuildDefinitionTaskGroupScanner.MetaTaskDefinitionType,
                        StringComparison.OrdinalIgnoreCase))
                {
                    var name = taskGroup["name"]?.GetValue<string>() ?? pair.Key;
                    throw new KnownException(
                        $"Task group '{name}' (id: {pair.Key}) contains nested task group references. " +
                        "Nested task groups are not supported. " +
                        "Refactor the parent task group first or unwind the nesting manually.");
                }
            }
        }
    }

    private void InlinePhase(JsonObject phase, string? taskGroupIdFilter, InlineResult result)
    {
        if (phase["steps"] is not JsonArray oldSteps)
        {
            return;
        }

        var newSteps = new JsonArray();

        foreach (var step in oldSteps)
        {
            if (step is not JsonObject stepObj)
            {
                if (step != null)
                {
                    newSteps.Add(DeepClone(step));
                }
                continue;
            }

            var taskGroupId = GetMetaTaskGroupId(stepObj);
            var matchesFilter = taskGroupId != null &&
                (taskGroupIdFilter == null ||
                 string.Equals(taskGroupId, taskGroupIdFilter, StringComparison.OrdinalIgnoreCase));

            if (taskGroupId == null || !matchesFilter)
            {
                newSteps.Add(DeepClone(stepObj));
                continue;
            }

            if (!_taskGroupsById.TryGetValue(taskGroupId, out var taskGroup))
            {
                newSteps.Add(DeepClone(stepObj));
                continue;
            }

            var disabledOriginal = (JsonObject)DeepClone(stepObj);
            disabledOriginal["enabled"] = false;
            var existingDisplay = disabledOriginal["displayName"]?.GetValue<string>() ?? string.Empty;
            disabledOriginal["displayName"] = InlinedDisplayNamePrefix + existingDisplay;
            newSteps.Add(disabledOriginal);

            var paramValues = ComputeEffectiveParameterValues(taskGroup, stepObj["inputs"]);

            if (taskGroup["tasks"] is JsonArray taskGroupSteps)
            {
                foreach (var tgStep in taskGroupSteps)
                {
                    if (tgStep is not JsonObject tgStepObj)
                    {
                        continue;
                    }

                    var clone = (JsonObject)DeepClone(tgStepObj);
                    SubstituteParameters(clone, paramValues);
                    newSteps.Add(clone);
                }
            }

            result.InlinedReferenceCount++;
            if (!result.InlinedTaskGroupIds.Contains(taskGroupId, StringComparer.OrdinalIgnoreCase))
            {
                result.InlinedTaskGroupIds.Add(taskGroupId);
            }
        }

        phase["steps"] = newSteps;
    }

    private static string? GetMetaTaskGroupId(JsonObject step)
    {
        var task = step["task"] as JsonObject;
        if (task == null)
        {
            return null;
        }

        var defType = task["definitionType"]?.GetValue<string>();
        if (!string.Equals(defType, BuildDefinitionTaskGroupScanner.MetaTaskDefinitionType,
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return task["id"]?.GetValue<string>();
    }

    private static Dictionary<string, string> ComputeEffectiveParameterValues(
        JsonNode taskGroup, JsonNode? buildStepInputs)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);

        if (taskGroup["inputs"] is not JsonArray paramDefs)
        {
            return values;
        }

        var callerInputs = buildStepInputs as JsonObject;

        foreach (var paramDef in paramDefs)
        {
            if (paramDef is not JsonObject paramObj)
            {
                continue;
            }

            var name = paramObj["name"]?.GetValue<string>();
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            var defaultValue = paramObj["defaultValue"]?.GetValue<string>() ?? string.Empty;

            string? callerValue = null;
            if (callerInputs != null && callerInputs.TryGetPropertyValue(name, out var node))
            {
                if (node is JsonValue jv && jv.TryGetValue<string>(out var s))
                {
                    callerValue = s;
                }
            }

            values[name] = string.IsNullOrEmpty(callerValue) ? defaultValue : callerValue;
        }

        return values;
    }

    private static void SubstituteParameters(JsonObject step, IReadOnlyDictionary<string, string> paramValues)
    {
        if (paramValues.Count == 0)
        {
            return;
        }

        SubstituteStringProperty(step, "displayName", paramValues);
        SubstituteStringProperty(step, "condition", paramValues);

        if (step["inputs"] is JsonObject inputs)
        {
            SubstituteInDictionary(inputs, paramValues);
        }

        if (step["environment"] is JsonObject environment)
        {
            SubstituteInDictionary(environment, paramValues);
        }
    }

    private static void SubstituteStringProperty(
        JsonObject obj, string propertyName, IReadOnlyDictionary<string, string> paramValues)
    {
        if (!obj.TryGetPropertyValue(propertyName, out var node))
        {
            return;
        }

        if (node is JsonValue jv && jv.TryGetValue<string>(out var current))
        {
            var replaced = ReplaceMacros(current, paramValues);
            if (!ReferenceEquals(replaced, current))
            {
                obj[propertyName] = replaced;
            }
        }
    }

    private static void SubstituteInDictionary(
        JsonObject dictionary, IReadOnlyDictionary<string, string> paramValues)
    {
        var keys = dictionary.Select(kvp => kvp.Key).ToList();

        foreach (var key in keys)
        {
            var value = dictionary[key];
            if (value is JsonValue jv && jv.TryGetValue<string>(out var current))
            {
                var replaced = ReplaceMacros(current, paramValues);
                if (!ReferenceEquals(replaced, current))
                {
                    dictionary[key] = replaced;
                }
            }
        }
    }

    private static string ReplaceMacros(string input, IReadOnlyDictionary<string, string> paramValues)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var result = input;
        foreach (var pair in paramValues)
        {
            var token = $"$({pair.Key})";
            if (result.Contains(token, StringComparison.Ordinal))
            {
                result = result.Replace(token, pair.Value, StringComparison.Ordinal);
            }
        }

        return result;
    }

    private static JsonNode DeepClone(JsonNode node)
    {
        var json = node.ToJsonString();
        return JsonNode.Parse(json) ??
            throw new InvalidOperationException("Failed to deep-clone JSON node.");
    }
}
