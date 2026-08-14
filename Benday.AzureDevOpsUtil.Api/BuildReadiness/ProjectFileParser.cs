using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Benday.AzureDevOpsUtil.Api.BuildReadiness;

public class ProjectFileParser
{
    private static readonly Regex WindowsAbsolutePathPattern = new(
        @"[A-Za-z]:\\[^\s""<>]+", RegexOptions.Compiled);

    private static readonly Regex UnixHardcodedPathPattern = new(
        @"/(Users|home|opt|var|tmp)/[^\s""<>]+", RegexOptions.Compiled);

    public ProjectFileAnalysisResult ParseProjectFile(
        string content, string projectFilePath, bool hasPackagesConfig = false)
    {
        var result = new ProjectFileAnalysisResult
        {
            Path = projectFilePath,
            FileName = GetFileName(projectFilePath),
            ProjectType = GetProjectType(projectFilePath)
        };

        if (string.IsNullOrWhiteSpace(content))
        {
            return result;
        }

        XDocument doc;

        try
        {
            doc = XDocument.Parse(content);
        }
        catch
        {
            return result;
        }

        ExtractPackageReferences(doc, result);
        ExtractProjectReferences(doc, result);
        ExtractHintPaths(doc, result);
        ExtractTargetFrameworks(doc, result);
        DetectHardcodedPaths(content, result);

        DetermineNuGetManagementStyle(result, hasPackagesConfig);

        return result;
    }

    /// <summary>
    /// Finds elements by local name regardless of namespace.  Project files
    /// written before the SDK-style format carry the MSBuild namespace
    /// (xmlns="http://schemas.microsoft.com/developer/msbuild/2003"), and a
    /// plain name lookup matches nothing in those documents.  Anything coming
    /// out of TFVC is likely to be in that older format.
    /// </summary>
    private static IEnumerable<XElement> ElementsNamed(XDocument doc, string localName)
    {
        return doc.Descendants().Where(x =>
            string.Equals(x.Name.LocalName, localName, StringComparison.Ordinal));
    }

    private static XElement? ChildNamed(XElement parent, string localName)
    {
        return parent.Elements().FirstOrDefault(x =>
            string.Equals(x.Name.LocalName, localName, StringComparison.Ordinal));
    }

    private void ExtractPackageReferences(XDocument doc, ProjectFileAnalysisResult result)
    {
        var packageRefs = ElementsNamed(doc, "PackageReference");

        foreach (var element in packageRefs)
        {
            var name = element.Attribute("Include")?.Value ?? string.Empty;
            var version = element.Attribute("Version")?.Value
                ?? ChildNamed(element, "Version")?.Value
                ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(name))
            {
                result.PackageReferences.Add(new NuGetPackageReference
                {
                    Name = name,
                    Version = version
                });
            }
        }
    }

    private void ExtractProjectReferences(XDocument doc, ProjectFileAnalysisResult result)
    {
        var projectRefs = ElementsNamed(doc, "ProjectReference");

        foreach (var element in projectRefs)
        {
            var includePath = element.Attribute("Include")?.Value ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(includePath))
            {
                var normalizedPath = NormalizePath(includePath);

                result.ExternalReferences.Add(new ExternalReference
                {
                    ReferenceType = "ProjectReference",
                    Path = normalizedPath,
                    Description = $"Project reference: {normalizedPath}"
                });
            }
        }
    }

    private void ExtractHintPaths(XDocument doc, ProjectFileAnalysisResult result)
    {
        var references = ElementsNamed(doc, "Reference");

        foreach (var refElement in references)
        {
            var hintPath = ChildNamed(refElement, "HintPath");

            if (hintPath != null && !string.IsNullOrWhiteSpace(hintPath.Value))
            {
                var normalizedPath = NormalizePath(hintPath.Value);

                result.ExternalReferences.Add(new ExternalReference
                {
                    ReferenceType = "HintPath",
                    Path = normalizedPath,
                    Description = $"Assembly reference via HintPath: {normalizedPath}"
                });
            }
        }
    }

    private void ExtractTargetFrameworks(XDocument doc, ProjectFileAnalysisResult result)
    {
        var targetFramework = ElementsNamed(doc, "TargetFramework").FirstOrDefault()?.Value;
        var targetFrameworks = ElementsNamed(doc, "TargetFrameworks").FirstOrDefault()?.Value;

        if (!string.IsNullOrWhiteSpace(targetFrameworks))
        {
            result.TargetFrameworks.AddRange(
                targetFrameworks.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(tf => tf.Trim()));
        }
        else if (!string.IsNullOrWhiteSpace(targetFramework))
        {
            result.TargetFrameworks.Add(targetFramework.Trim());
        }
    }

    private void DetectHardcodedPaths(string content, ProjectFileAnalysisResult result)
    {
        var windowsMatches = WindowsAbsolutePathPattern.Matches(content);

        foreach (Match match in windowsMatches)
        {
            result.HardcodedPaths.Add(match.Value);
        }

        var unixMatches = UnixHardcodedPathPattern.Matches(content);

        foreach (Match match in unixMatches)
        {
            result.HardcodedPaths.Add(match.Value);
        }
    }

    private void DetermineNuGetManagementStyle(
        ProjectFileAnalysisResult result, bool hasPackagesConfig)
    {
        var hasPackageReference = result.PackageReferences.Count > 0;

        if (hasPackageReference && hasPackagesConfig)
        {
            result.NuGetManagementStyle = "Mixed";
        }
        else if (hasPackageReference)
        {
            result.NuGetManagementStyle = "PackageReference";
        }
        else if (hasPackagesConfig)
        {
            result.NuGetManagementStyle = "PackagesConfig";
        }
        else
        {
            result.NuGetManagementStyle = "None";
        }
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static string GetFileName(string path)
    {
        var normalized = NormalizePath(path);
        var lastSlash = normalized.LastIndexOf('/');
        return lastSlash >= 0 ? normalized[(lastSlash + 1)..] : normalized;
    }

    private static string GetProjectType(string path)
    {
        var extension = System.IO.Path.GetExtension(path);
        return extension?.TrimStart('.') ?? string.Empty;
    }
}
