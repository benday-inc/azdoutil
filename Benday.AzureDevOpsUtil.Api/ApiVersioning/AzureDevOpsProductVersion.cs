namespace Benday.AzureDevOpsUtil.Api.ApiVersioning;

/// <summary>
/// Names the product behind an api-version ceiling.
///
/// Nothing an Azure DevOps collection returns states its own product version --
/// not connectionData, not any response header -- so the api-version it tops
/// out at is the closest thing to an answer.  Major api-version releases line
/// up with on-prem releases, which is what makes this readable at all.
///
/// This is for display.  Decisions are made from the catalog, which is measured
/// rather than inferred.
/// </summary>
public static class AzureDevOpsProductVersion
{
    /// <summary>
    /// The release line behind a four-part build number, or an empty string
    /// when it is not one this knows.
    ///
    /// Far more precise than <see cref="Describe"/>, because the first two
    /// octets identify the update train rather than just the wave: 17.143 and
    /// 17.153 are both "Azure DevOps Server 2019" by api-version, but they are
    /// RTW and Update 1.  Microsoft publishes no first-party build-to-release
    /// mapping, so this is assembled from their REST version table and the
    /// community release list.
    /// </summary>
    public static string DescribeBuild(string? buildNumber)
    {
        if (string.IsNullOrWhiteSpace(buildNumber) == true)
        {
            return string.Empty;
        }

        var octets = buildNumber.Split('.');

        if (octets.Length < 2 ||
            int.TryParse(octets[0], out var major) == false ||
            int.TryParse(octets[1], out var minor) == false)
        {
            return string.Empty;
        }

        return (major, minor) switch
        {
            (14, _) => "Team Foundation Server 2015",
            (15, _) => "Team Foundation Server 2017",
            (16, _) => "Team Foundation Server 2018",
            (17, <= 143) => "Azure DevOps Server 2019 (RTW line)",
            (17, _) => "Azure DevOps Server 2019 Update 1 or later",
            (18, <= 170) => "Azure DevOps Server 2020 (RTW line)",
            (18, _) => "Azure DevOps Server 2020.1 or later",
            (19, <= 205) => "Azure DevOps Server 2022 (RTW line)",
            (19, <= 225) => "Azure DevOps Server 2022.1",
            (19, _) => "Azure DevOps Server 2022.2 or later",
            (20, _) => "Azure DevOps Server 2025",
            _ => string.Empty
        };
    }

    public static string Describe(ApiVersion maxReleasedVersion, bool isHosted)
    {
        if (isHosted == true)
        {
            return "Azure DevOps Services";
        }

        if (maxReleasedVersion.IsEmpty == true)
        {
            return "unknown";
        }

        return (maxReleasedVersion.Major, maxReleasedVersion.Minor) switch
        {
            (>= 8, _) => "newer than Azure DevOps Server 2022.1",
            (7, >= 2) => "Azure DevOps Server 2022.2 or newer",
            (7, 1) => "Azure DevOps Server 2022.1",
            (7, 0) => "Azure DevOps Server 2022",
            (6, _) => "Azure DevOps Server 2020",
            (5, _) => "Azure DevOps Server 2019",
            (4, _) => "Team Foundation Server 2018",
            (3, _) => "Team Foundation Server 2017",
            (2, _) => "Team Foundation Server 2015",
            _ => "unknown"
        };
    }
}
