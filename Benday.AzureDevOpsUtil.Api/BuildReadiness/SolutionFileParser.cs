using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Benday.AzureDevOpsUtil.Api.BuildReadiness;

public class SolutionFileParser
{
    private const string SolutionFolderTypeGuid = "2150E333-8FDC-42A3-9474-1A3956D46DE8";

    private static readonly Regex SlnProjectPattern = new(
        @"^Project\(""\{([^}]*)\}""\)\s*=\s*""([^""]*)""\s*,\s*""([^""]*)""\s*,\s*""[^""]*""",
        RegexOptions.Compiled);

    public List<SolutionProjectEntry> ParseSolutionFile(string content, bool isSlnx = false)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new List<SolutionProjectEntry>();
        }

        return isSlnx ? ParseSlnxContent(content) : ParseSlnContent(content);
    }

    private List<SolutionProjectEntry> ParseSlnContent(string content)
    {
        var results = new List<SolutionProjectEntry>();

        using var reader = new StringReader(content);

        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            if (!line.StartsWith("Project("))
            {
                continue;
            }

            var match = SlnProjectPattern.Match(line);

            if (!match.Success)
            {
                continue;
            }

            var typeGuid = match.Groups[1].Value;
            var name = match.Groups[2].Value;
            var relativePath = match.Groups[3].Value;

            if (string.Equals(typeGuid, SolutionFolderTypeGuid, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            results.Add(new SolutionProjectEntry
            {
                Name = name,
                RelativePath = NormalizePath(relativePath),
                ProjectTypeGuid = typeGuid
            });
        }

        return results;
    }

    private List<SolutionProjectEntry> ParseSlnxContent(string content)
    {
        var results = new List<SolutionProjectEntry>();

        XDocument doc;

        try
        {
            doc = XDocument.Parse(content);
        }
        catch
        {
            return results;
        }

        var projectElements = doc.Descendants("Project");

        foreach (var element in projectElements)
        {
            var path = element.Attribute("Path")?.Value;

            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            var name = System.IO.Path.GetFileNameWithoutExtension(path);

            results.Add(new SolutionProjectEntry
            {
                Name = name,
                RelativePath = NormalizePath(path),
                ProjectTypeGuid = string.Empty
            });
        }

        return results;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }
}

public class SolutionProjectEntry
{
    public string Name { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string ProjectTypeGuid { get; set; } = string.Empty;
}
