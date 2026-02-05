namespace Benday.AzureDevOpsUtil.Api.BuildReadiness;

public class BuildReadinessAnalyzer
{
    private readonly List<string> _filePaths;
    private readonly IFileContentProvider _contentProvider;
    private readonly SolutionFileParser _solutionParser;
    private readonly ProjectFileParser _projectParser;

    private static readonly string[] SolutionExtensions = { ".sln", ".slnx" };

    private static readonly string[] ProjectExtensions =
    {
        ".csproj", ".vbproj", ".fsproj", ".sqlproj",
        ".dcproj", ".esproj", ".wixproj", ".shproj"
    };

    private static readonly string[] BuildConfigFileNames =
    {
        "nuget.config", "NuGet.config", "NuGet.Config",
        "Directory.Build.props", "Directory.Build.targets",
        "global.json"
    };

    public BuildReadinessAnalyzer(List<string> filePaths, IFileContentProvider contentProvider)
    {
        _filePaths = filePaths ?? throw new ArgumentNullException(nameof(filePaths));
        _contentProvider = contentProvider ?? throw new ArgumentNullException(nameof(contentProvider));
        _solutionParser = new SolutionFileParser();
        _projectParser = new ProjectFileParser();
    }

    public async Task<RepositoryAnalysisResult> AnalyzeAsync(
        string projectName, string repositoryName, string repositoryId)
    {
        var result = new RepositoryAnalysisResult
        {
            ProjectName = projectName,
            RepositoryName = repositoryName,
            RepositoryId = repositoryId
        };

        if (_filePaths.Count == 0)
        {
            result.HasError = true;
            result.ErrorMessage = "Repository appears to be empty (no files found).";
            return result;
        }

        DetectSubmodules(result);
        DetectPackagesConfig(result);
        DetectBuildConfigFiles(result);

        await AnalyzeSolutionFiles(result);
        await AnalyzeProjectFiles(result);

        AggregateDistinctPackages(result);
        DetectSolutionRootViolations(result);

        result.HasPackageReference = result.ProjectFiles
            .Any(p => p.NuGetManagementStyle is "PackageReference" or "Mixed");

        return result;
    }

    private void DetectSubmodules(RepositoryAnalysisResult result)
    {
        result.HasSubmodules = _filePaths.Any(p =>
            GetFileName(p).Equals(".gitmodules", StringComparison.OrdinalIgnoreCase));
    }

    private void DetectPackagesConfig(RepositoryAnalysisResult result)
    {
        result.HasPackagesConfig = _filePaths.Any(p =>
            GetFileName(p).Equals("packages.config", StringComparison.OrdinalIgnoreCase));
    }

    private void DetectBuildConfigFiles(RepositoryAnalysisResult result)
    {
        foreach (var filePath in _filePaths)
        {
            var fileName = GetFileName(filePath);

            if (BuildConfigFileNames.Any(name =>
                string.Equals(fileName, name, StringComparison.OrdinalIgnoreCase)))
            {
                result.BuildConfigFiles.Add(new BuildConfigFileInfo
                {
                    FileName = fileName,
                    Path = filePath
                });
            }
        }
    }

    private async Task AnalyzeSolutionFiles(RepositoryAnalysisResult result)
    {
        var solutionFiles = _filePaths
            .Where(p => SolutionExtensions.Any(ext =>
                p.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var solutionPath in solutionFiles)
        {
            var content = await _contentProvider.GetFileContentAsync(solutionPath);

            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            var isSlnx = solutionPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase);
            var entries = _solutionParser.ParseSolutionFile(content, isSlnx);

            var solutionRoot = GetDirectoryPath(solutionPath);

            var solutionResult = new SolutionAnalysisResult
            {
                Path = solutionPath,
                FileName = GetFileName(solutionPath),
                SolutionRoot = solutionRoot,
                ReferencedProjectPaths = entries.Select(e => e.RelativePath).ToList()
            };

            result.Solutions.Add(solutionResult);
        }
    }

    private async Task AnalyzeProjectFiles(RepositoryAnalysisResult result)
    {
        var projectFiles = _filePaths
            .Where(p => ProjectExtensions.Any(ext =>
                p.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var projectPath in projectFiles)
        {
            var content = await _contentProvider.GetFileContentAsync(projectPath);

            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            var projectDir = GetDirectoryPath(projectPath);
            var hasPackagesConfig = _filePaths.Any(p =>
                p.Equals(CombinePath(projectDir, "packages.config"), StringComparison.OrdinalIgnoreCase));

            var projectResult = _projectParser.ParseProjectFile(content, projectPath, hasPackagesConfig);

            result.ProjectFiles.Add(projectResult);
        }
    }

    private void DetectSolutionRootViolations(RepositoryAnalysisResult result)
    {
        foreach (var solution in result.Solutions)
        {
            foreach (var projectRelativePath in solution.ReferencedProjectPaths)
            {
                var resolvedPath = ResolvePath(solution.SolutionRoot, projectRelativePath);

                if (!IsUnderPath(resolvedPath, solution.SolutionRoot))
                {
                    solution.SolutionRootViolations.Add(projectRelativePath);
                }
            }
        }
    }

    private void AggregateDistinctPackages(RepositoryAnalysisResult result)
    {
        var allPackages = result.ProjectFiles
            .SelectMany(p => p.PackageReferences)
            .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => new NuGetPackageReference
            {
                Name = g.Key,
                Version = string.Join(", ", g.Select(p => p.Version).Distinct())
            })
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        result.AllDistinctPackageReferences = allPackages;
    }

    private static string ResolvePath(string basePath, string relativePath)
    {
        var normalizedRelative = relativePath.Replace('\\', '/');
        var combined = CombinePath(basePath, normalizedRelative);

        var parts = combined.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var resolved = new Stack<string>();

        foreach (var part in parts)
        {
            if (part == "..")
            {
                if (resolved.Count > 0)
                {
                    resolved.Pop();
                }
            }
            else if (part != ".")
            {
                resolved.Push(part);
            }
        }

        var result = "/" + string.Join("/", resolved.Reverse());
        return result;
    }

    private static bool IsUnderPath(string path, string basePath)
    {
        var normalizedPath = path.TrimEnd('/');
        var normalizedBase = basePath.TrimEnd('/');

        if (string.IsNullOrEmpty(normalizedBase) || normalizedBase == "/")
        {
            return true;
        }

        return normalizedPath.StartsWith(normalizedBase + "/", StringComparison.OrdinalIgnoreCase)
            || normalizedPath.Equals(normalizedBase, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetDirectoryPath(string filePath)
    {
        var normalized = filePath.Replace('\\', '/');
        var lastSlash = normalized.LastIndexOf('/');
        return lastSlash >= 0 ? normalized[..lastSlash] : "/";
    }

    private static string GetFileName(string filePath)
    {
        var normalized = filePath.Replace('\\', '/');
        var lastSlash = normalized.LastIndexOf('/');
        return lastSlash >= 0 ? normalized[(lastSlash + 1)..] : normalized;
    }

    private static string CombinePath(string basePath, string relativePath)
    {
        var normalizedBase = basePath.TrimEnd('/');
        var normalizedRelative = relativePath.TrimStart('/');
        return normalizedBase + "/" + normalizedRelative;
    }
}
