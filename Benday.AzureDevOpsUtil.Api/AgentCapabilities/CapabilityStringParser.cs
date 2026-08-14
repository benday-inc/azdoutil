namespace Benday.AzureDevOpsUtil.Api.AgentCapabilities;

/// <summary>
/// Parses the inline <c>/capabilities</c> value on <c>setagentcapabilities</c>.
/// The format is <c>name=value</c> pairs separated by semicolons, e.g.
/// <c>VisualStudio=2022;SpecialSoftware=true</c>.  A pair with no <c>=</c> is
/// treated as a capability with an empty value, which matches how an "exists"
/// demand is satisfied.
/// </summary>
public static class CapabilityStringParser
{
    public static Dictionary<string, string> Parse(string? value)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(value) == true)
        {
            return result;
        }

        var pairs = value.Split(';', StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);

        foreach (var pair in pairs)
        {
            var separatorIndex = pair.IndexOf('=');

            if (separatorIndex < 0)
            {
                result[pair.Trim()] = string.Empty;
            }
            else
            {
                var key = pair.Substring(0, separatorIndex).Trim();
                var pairValue = pair.Substring(separatorIndex + 1).Trim();

                if (string.IsNullOrEmpty(key) == false)
                {
                    result[key] = pairValue;
                }
            }
        }

        return result;
    }
}
