using Benday.AzureDevOpsUtil.Api.BuildReadiness;
using Benday.AzureDevOpsUtil.Api.Messages;

namespace Benday.AzureDevOpsUtil.Api.TfvcAssessment;

public class TfvcSolutionInfo
{
    public string Path { get; set; } = string.Empty;

    /// <summary>The folder holding the solution file.</summary>
    public string RootFolder { get; set; } = string.Empty;

    /// <summary>Resolved server paths of the projects the solution lists.</summary>
    public List<string> ProjectPaths { get; set; } = new();
}

public class TfvcProjectInfo
{
    public string Path { get; set; } = string.Empty;

    public bool UsesPackagesConfig { get; set; }

    public List<string> TargetFrameworks { get; set; } = new();

    /// <summary>Resolved server paths of the projects this project references.</summary>
    public List<string> ProjectReferences { get; set; } = new();

    /// <summary>Assemblies referenced by hint path rather than by package.</summary>
    public int BinaryReferenceCount { get; set; }
}

/// <summary>
/// A project reference that resolves outside the solution that contains it.
/// </summary>
public class CrossSolutionReference
{
    public string SolutionPath { get; set; } = string.Empty;

    public string FromProject { get; set; } = string.Empty;

    public string ToProject { get; set; } = string.Empty;
}

/// <summary>
/// A project referenced from more than one solution.
/// </summary>
public class SharedProjectUsage
{
    public string ProjectPath { get; set; } = string.Empty;

    public List<string> SolutionPaths { get; set; } = new();

    public int SolutionCount => SolutionPaths.Count;
}

public class TfvcSolutionAnalysisResult
{
    public List<TfvcSolutionInfo> Solutions { get; set; } = new();

    public List<TfvcProjectInfo> Projects { get; set; } = new();

    public List<CrossSolutionReference> CrossSolutionReferences { get; set; } = new();

    public List<SharedProjectUsage> SharedProjects { get; set; } = new();

    /// <summary>Files that were found but could not be read.</summary>
    public List<string> UnreadableFiles { get; set; } = new();

    public int ProjectsUsingPackagesConfig =>
        Projects.Count(x => x.UsesPackagesConfig == true);
}

/// <summary>
/// Reads the solutions and projects straight off the server and works out how
/// they are wired to each other.
///
/// The parsers in BuildReadiness take file content as a string, so there is no
/// need for a local workspace or for a second tool: the file list comes from
/// the item listing that has already been fetched, and each file is read
/// through the TFVC items API.
/// </summary>
public class TfvcSolutionAnalysisService
{
    private static readonly string[] SolutionExtensions = { ".sln", ".slnx" };

    private static readonly string[] ProjectExtensions =
    {
        ".csproj", ".vbproj", ".fsproj", ".sqlproj",
        ".dcproj", ".esproj", ".wixproj", ".shproj"
    };

    private readonly SolutionFileParser _solutionParser = new();
    private readonly ProjectFileParser _projectParser = new();

    public Action<string>? ProgressCallback { get; set; }

    public async Task<TfvcSolutionAnalysisResult> AnalyzeAsync(
        ITfvcApiClient client,
        string projectName,
        IReadOnlyList<TfvcItemInfo>? items)
    {
        ArgumentNullException.ThrowIfNull(client);

        var result = new TfvcSolutionAnalysisResult();

        if (items == null || items.Count == 0)
        {
            return result;
        }

        var filePaths = items
            .Where(x => x.IsFolder != true)
            .Select(x => TfvcPath.Normalize(x.Path))
            .ToList();

        var packagesConfigFolders = new HashSet<string>(
            filePaths
                .Where(x => string.Equals(
                    TfvcPath.GetName(x), "packages.config", StringComparison.OrdinalIgnoreCase))
                .Select(x => TfvcPath.GetParent(x) ?? TfvcPath.Root),
            StringComparer.OrdinalIgnoreCase);

        await ReadSolutionsAsync(client, projectName, filePaths, result);

        await ReadProjectsAsync(client, projectName, filePaths, packagesConfigFolders, result);

        BuildCrossSolutionReferences(result);

        BuildSharedProjects(result);

        return result;
    }

    private async Task ReadSolutionsAsync(
        ITfvcApiClient client,
        string projectName,
        List<string> filePaths,
        TfvcSolutionAnalysisResult result)
    {
        var solutionPaths = filePaths
            .Where(x => HasExtension(x, SolutionExtensions))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var solutionPath in solutionPaths)
        {
            ProgressCallback?.Invoke($"Reading solution {solutionPath}...");

            var content = await client.GetFileContentAsync(projectName, solutionPath);

            if (string.IsNullOrWhiteSpace(content) == true)
            {
                result.UnreadableFiles.Add(solutionPath);
                continue;
            }

            var rootFolder = TfvcPath.GetParent(solutionPath) ?? TfvcPath.Root;

            var isSlnx = string.Equals(
                TfvcContentScanner.GetExtension(solutionPath), ".slnx",
                StringComparison.OrdinalIgnoreCase);

            var solution = new TfvcSolutionInfo
            {
                Path = solutionPath,
                RootFolder = rootFolder
            };

            foreach (var entry in _solutionParser.ParseSolutionFile(content, isSlnx))
            {
                var resolved = TfvcPath.Combine(rootFolder, entry.RelativePath);

                if (resolved == null)
                {
                    continue;
                }

                solution.ProjectPaths.Add(resolved);
            }

            result.Solutions.Add(solution);
        }
    }

    private async Task ReadProjectsAsync(
        ITfvcApiClient client,
        string projectName,
        List<string> filePaths,
        HashSet<string> packagesConfigFolders,
        TfvcSolutionAnalysisResult result)
    {
        var projectPaths = filePaths
            .Where(x => HasExtension(x, ProjectExtensions))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var projectPath in projectPaths)
        {
            ProgressCallback?.Invoke($"Reading project {projectPath}...");

            var content = await client.GetFileContentAsync(projectName, projectPath);

            if (string.IsNullOrWhiteSpace(content) == true)
            {
                result.UnreadableFiles.Add(projectPath);
                continue;
            }

            var folder = TfvcPath.GetParent(projectPath) ?? TfvcPath.Root;

            var hasPackagesConfig = packagesConfigFolders.Contains(folder);

            var parsed = _projectParser.ParseProjectFile(content, projectPath, hasPackagesConfig);

            var info = new TfvcProjectInfo
            {
                Path = projectPath,
                UsesPackagesConfig = hasPackagesConfig,
                TargetFrameworks = parsed.TargetFrameworks,
                BinaryReferenceCount = parsed.ExternalReferences.Count(
                    x => string.Equals(x.ReferenceType, "HintPath", StringComparison.Ordinal))
            };

            foreach (var reference in parsed.ExternalReferences)
            {
                if (string.Equals(
                    reference.ReferenceType, "ProjectReference", StringComparison.Ordinal) == false)
                {
                    continue;
                }

                var resolved = TfvcPath.Combine(folder, reference.Path);

                if (resolved == null)
                {
                    continue;
                }

                info.ProjectReferences.Add(resolved);
            }

            result.Projects.Add(info);
        }
    }

    /// <summary>
    /// A project reference that lands outside the solution's own folder does
    /// not survive a split into separate repositories.
    /// </summary>
    private void BuildCrossSolutionReferences(TfvcSolutionAnalysisResult result)
    {
        var projectsByPath = result.Projects.ToDictionary(
            x => x.Path, StringComparer.OrdinalIgnoreCase);

        foreach (var solution in result.Solutions)
        {
            foreach (var projectPath in solution.ProjectPaths)
            {
                if (projectsByPath.TryGetValue(projectPath, out var project) == false)
                {
                    continue;
                }

                foreach (var reference in project.ProjectReferences)
                {
                    if (TfvcPath.IsSameOrUnder(reference, solution.RootFolder) == true)
                    {
                        continue;
                    }

                    result.CrossSolutionReferences.Add(new CrossSolutionReference
                    {
                        SolutionPath = solution.Path,
                        FromProject = projectPath,
                        ToProject = reference
                    });
                }
            }
        }
    }

    /// <summary>
    /// A project reachable from more than one solution is shared at the source
    /// level.  Reachability walks project references, because a project pulled
    /// in indirectly is just as coupled as one listed in the solution.
    /// </summary>
    private void BuildSharedProjects(TfvcSolutionAnalysisResult result)
    {
        var projectsByPath = result.Projects.ToDictionary(
            x => x.Path, StringComparer.OrdinalIgnoreCase);

        var solutionsByProject =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var solution in result.Solutions)
        {
            foreach (var reachable in GetReachableProjects(solution, projectsByPath))
            {
                if (solutionsByProject.TryGetValue(reachable, out var solutions) == false)
                {
                    solutions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    solutionsByProject[reachable] = solutions;
                }

                solutions.Add(solution.Path);
            }
        }

        result.SharedProjects = solutionsByProject
            .Where(x => x.Value.Count > 1)
            .Select(x => new SharedProjectUsage
            {
                ProjectPath = x.Key,
                SolutionPaths = x.Value
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .ToList()
            })
            .OrderByDescending(x => x.SolutionCount)
            .ThenBy(x => x.ProjectPath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private HashSet<string> GetReachableProjects(
        TfvcSolutionInfo solution, Dictionary<string, TfvcProjectInfo> projectsByPath)
    {
        var reached = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var pending = new Queue<string>();

        foreach (var path in solution.ProjectPaths)
        {
            pending.Enqueue(path);
        }

        while (pending.Count > 0)
        {
            var current = pending.Dequeue();

            if (reached.Add(current) == false)
            {
                continue;
            }

            if (projectsByPath.TryGetValue(current, out var project) == false)
            {
                continue;
            }

            foreach (var reference in project.ProjectReferences)
            {
                if (reached.Contains(reference) == false)
                {
                    pending.Enqueue(reference);
                }
            }
        }

        return reached;
    }

    private static bool HasExtension(string path, string[] extensions)
    {
        var extension = TfvcContentScanner.GetExtension(path);

        return extensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}
