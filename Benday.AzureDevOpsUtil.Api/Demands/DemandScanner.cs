using System.Text.Json;
using System.Text.Json.Nodes;

namespace Benday.AzureDevOpsUtil.Api.Demands;

/// <summary>
/// Finds the demands stored in a build or release definition's JSON.  Demands
/// live in more than one place — at the root of a build definition, on a build
/// job/phase, and under each release deploy phase's <c>deploymentInput</c> — so
/// rather than model every nesting this walks the document and collects every
/// <c>demands</c> array it finds.  It is a pure function over the JSON text with
/// no I/O, which is what makes it testable.
/// </summary>
public static class DemandScanner
{
    /// <summary>
    /// The distinct demand expressions in the definition, in the order first
    /// seen.  Each entry is the demand as authored, e.g. <c>msbuild</c> or
    /// <c>Agent.OS -equals Windows_NT</c>.  Returns an empty list when the JSON
    /// has no demands or cannot be parsed.
    /// </summary>
    public static IReadOnlyList<string> Scan(string? json)
    {
        var results = new List<string>();

        if (string.IsNullOrWhiteSpace(json) == true)
        {
            return results;
        }

        JsonNode? root;

        try
        {
            root = JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return results;
        }

        if (root == null)
        {
            return results;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Walk(root, results, seen);

        return results;
    }

    private static void Walk(JsonNode node, List<string> results, HashSet<string> seen)
    {
        if (node is JsonObject obj)
        {
            foreach (var pair in obj)
            {
                if (string.Equals(pair.Key, "demands", StringComparison.OrdinalIgnoreCase) == true &&
                    pair.Value is JsonArray demands)
                {
                    CollectDemands(demands, results, seen);
                }

                if (pair.Value != null)
                {
                    Walk(pair.Value, results, seen);
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var element in array)
            {
                if (element != null)
                {
                    Walk(element, results, seen);
                }
            }
        }
    }

    private static void CollectDemands(JsonArray demands, List<string> results, HashSet<string> seen)
    {
        foreach (var element in demands)
        {
            var value = DescribeDemand(element);

            if (string.IsNullOrWhiteSpace(value) == true)
            {
                continue;
            }

            if (seen.Add(value) == true)
            {
                results.Add(value);
            }
        }
    }

    /// <summary>
    /// A demand is usually a plain string, but the object model form is
    /// <c>{ "name": "...", "value": "..." }</c>, so both are handled.
    /// </summary>
    private static string DescribeDemand(JsonNode? element)
    {
        if (element is JsonValue value)
        {
            return value.ToString().Trim();
        }

        if (element is JsonObject obj)
        {
            var name = obj["name"]?.ToString().Trim() ?? string.Empty;
            var demandValue = obj["value"]?.ToString().Trim();

            if (string.IsNullOrEmpty(demandValue) == true)
            {
                return name;
            }

            return $"{name} -equals {demandValue}";
        }

        return string.Empty;
    }
}
