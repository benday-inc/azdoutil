using Benday.AzureDevOpsUtil.Api.GitRemotes;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class GitRepositoryLocatorFixture
{
    private string _TempRoot = string.Empty;

    [TestInitialize]
    public void OnTestInitialize()
    {
        _TempRoot = Path.Combine(
            Path.GetTempPath(), "azdoutil-git-locator-" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_TempRoot);
    }

    [TestCleanup]
    public void OnTestCleanup()
    {
        if (Directory.Exists(_TempRoot) == true)
        {
            Directory.Delete(_TempRoot, true);
        }
    }

    private const string ConfigWithOrigin = """
[core]
	repositoryformatversion = 0
	filemode = true
[remote "origin"]
	url = https://dev.azure.com/benday/MyProject/_git/MyRepo
	fetch = +refs/heads/*:refs/remotes/origin/*
[branch "main"]
	remote = origin
	merge = refs/heads/main
""";

    private string CreateRepository(string configContent, params string[] nestedFolders)
    {
        var gitDirectory = Path.Combine(_TempRoot, ".git");

        Directory.CreateDirectory(gitDirectory);

        File.WriteAllText(Path.Combine(gitDirectory, "config"), configContent);

        if (nestedFolders.Length == 0)
        {
            return _TempRoot;
        }

        var nested = Path.Combine(_TempRoot, Path.Combine(nestedFolders));

        Directory.CreateDirectory(nested);

        return nested;
    }

    [TestMethod]
    public void FindGitDirectory_FindsTheRepositoryRoot()
    {
        var start = CreateRepository(ConfigWithOrigin);

        var actual = GitRepositoryLocator.FindGitDirectory(start);

        Assert.IsNotNull(actual, "The repository should have been found.");
        Assert.AreEqual(
            Path.Combine(_TempRoot, ".git"), actual, "Wrong git directory.");
    }

    [TestMethod]
    public void FindGitDirectory_WalksUpFromANestedFolder()
    {
        var start = CreateRepository(ConfigWithOrigin, "src", "App", "Web");

        var actual = GitRepositoryLocator.FindGitDirectory(start);

        Assert.IsNotNull(actual, "The walk should reach the repository root.");
        Assert.AreEqual(Path.Combine(_TempRoot, ".git"), actual, "Wrong git directory.");
    }

    [TestMethod]
    public void FindGitDirectory_ReturnsNullOutsideARepository()
    {
        var outside = Path.Combine(_TempRoot, "not-a-repo");

        Directory.CreateDirectory(outside);

        Assert.IsNull(
            GitRepositoryLocator.FindGitDirectory(outside),
            "This directory is not inside a repository.");
    }

    [TestMethod]
    public void FindGitDirectory_FollowsAGitFileToItsRealLocation()
    {
        // Worktrees and submodules leave a file rather than a directory.
        var realGitDirectory = Path.Combine(_TempRoot, "actual-git-dir");

        Directory.CreateDirectory(realGitDirectory);

        var workingDirectory = Path.Combine(_TempRoot, "worktree");

        Directory.CreateDirectory(workingDirectory);

        File.WriteAllText(
            Path.Combine(workingDirectory, ".git"), $"gitdir: {realGitDirectory}");

        var actual = GitRepositoryLocator.FindGitDirectory(workingDirectory);

        Assert.AreEqual(realGitDirectory, actual, "The git file should have been followed.");
    }

    [TestMethod]
    public void FindGitDirectory_FollowsARelativeGitFile()
    {
        var realGitDirectory = Path.Combine(_TempRoot, "actual-git-dir");

        Directory.CreateDirectory(realGitDirectory);

        var workingDirectory = Path.Combine(_TempRoot, "worktree");

        Directory.CreateDirectory(workingDirectory);

        File.WriteAllText(
            Path.Combine(workingDirectory, ".git"), "gitdir: ../actual-git-dir");

        var actual = GitRepositoryLocator.FindGitDirectory(workingDirectory);

        Assert.AreEqual(
            realGitDirectory,
            actual?.TrimEnd(Path.DirectorySeparatorChar),
            "A relative git file should resolve against its own folder.");
    }

    [TestMethod]
    public void FindConfigFilePath_UsesTheMainRepositoryConfigForAWorktree()
    {
        // A worktree's git directory has no config of its own; commondir names
        // where the real one lives.
        var mainGitDirectory = Path.Combine(_TempRoot, "main", ".git");

        Directory.CreateDirectory(mainGitDirectory);

        File.WriteAllText(Path.Combine(mainGitDirectory, "config"), ConfigWithOrigin);

        var worktreeGitDirectory = Path.Combine(mainGitDirectory, "worktrees", "feature");

        Directory.CreateDirectory(worktreeGitDirectory);

        File.WriteAllText(
            Path.Combine(worktreeGitDirectory, "commondir"), "../..");

        var actual = GitRepositoryLocator.FindConfigFilePath(worktreeGitDirectory);

        Assert.IsNotNull(actual, "The shared config should have been found.");
        Assert.AreEqual(
            Path.Combine(mainGitDirectory, "config"),
            Path.GetFullPath(actual!),
            "Wrong config file.");
    }

    [TestMethod]
    public void FindRemoteUrl_ReadsTheOriginUrl()
    {
        var start = CreateRepository(ConfigWithOrigin, "src");

        var actual = GitRepositoryLocator.FindRemoteUrl(start);

        Assert.AreEqual(
            "https://dev.azure.com/benday/MyProject/_git/MyRepo", actual, "Wrong remote url.");
    }

    [TestMethod]
    public void FindRemoteUrl_ReturnsNullOutsideARepository()
    {
        var outside = Path.Combine(_TempRoot, "not-a-repo");

        Directory.CreateDirectory(outside);

        Assert.IsNull(GitRepositoryLocator.FindRemoteUrl(outside), "There is no repository here.");
    }

    [TestMethod]
    public void ParseRemoteUrl_ReadsTheNamedRemote()
    {
        const string Config = """
[remote "origin"]
	url = https://dev.azure.com/benday/MyProject/_git/MyRepo
[remote "upstream"]
	url = https://dev.azure.com/other/OtherProject/_git/OtherRepo
""";

        Assert.AreEqual(
            "https://dev.azure.com/benday/MyProject/_git/MyRepo",
            GitRepositoryLocator.ParseRemoteUrl(Config),
            "Wrong origin url.");

        Assert.AreEqual(
            "https://dev.azure.com/other/OtherProject/_git/OtherRepo",
            GitRepositoryLocator.ParseRemoteUrl(Config, "upstream"),
            "Wrong upstream url.");
    }

    [TestMethod]
    public void ParseRemoteUrl_IgnoresUrlsInOtherSections()
    {
        const string Config = """
[core]
	url = this-is-not-a-remote
[branch "main"]
	remote = origin
""";

        Assert.IsNull(
            GitRepositoryLocator.ParseRemoteUrl(Config),
            "A url outside the remote section is not the remote url.");
    }

    [TestMethod]
    public void ParseRemoteUrl_IgnoresComments()
    {
        const string Config = """
[remote "origin"]
	# url = https://dev.azure.com/commented/Out/_git/Repo
	url = https://dev.azure.com/benday/MyProject/_git/MyRepo
""";

        Assert.AreEqual(
            "https://dev.azure.com/benday/MyProject/_git/MyRepo",
            GitRepositoryLocator.ParseRemoteUrl(Config),
            "A commented out url should be ignored.");
    }

    [TestMethod]
    public void ParseRemoteUrl_MissingRemoteReturnsNull()
    {
        Assert.IsNull(
            GitRepositoryLocator.ParseRemoteUrl(ConfigWithOrigin, "nosuchremote"),
            "There is no such remote.");

        Assert.IsNull(GitRepositoryLocator.ParseRemoteUrl(null), "There is no config.");
        Assert.IsNull(GitRepositoryLocator.ParseRemoteUrl("   "), "There is no config.");
    }

    [TestMethod]
    public void FindRemoteUrl_ParsesEndToEnd()
    {
        var start = CreateRepository(ConfigWithOrigin, "src", "App");

        var remoteUrl = GitRepositoryLocator.FindRemoteUrl(start);

        var actual = GitRemoteUrlParser.Parse(remoteUrl);

        Assert.IsNotNull(actual, "The remote should have parsed.");
        Assert.AreEqual("MyProject", actual!.ProjectName, "Wrong project.");
        Assert.AreEqual("MyRepo", actual.RepositoryName, "Wrong repository.");
        Assert.AreEqual(
            "https://dev.azure.com/benday/", actual.CollectionUrl, "Wrong collection url.");
    }
}
