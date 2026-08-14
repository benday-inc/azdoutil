using Benday.AzureDevOpsUtil.Api.GitRemotes;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class GitRemoteUrlParserFixture
{
    [TestMethod]
    [DataRow(
        "https://dev.azure.com/benday/tfvc-demo-2024/_git/MyRepo",
        "https://dev.azure.com/benday/", "benday", "tfvc-demo-2024", "MyRepo")]
    [DataRow(
        "https://benday@dev.azure.com/benday/tfvc-demo-2024/_git/MyRepo",
        "https://dev.azure.com/benday/", "benday", "tfvc-demo-2024", "MyRepo")]
    [DataRow(
        "https://dev.azure.com/benday/tfvc-demo-2024/_git/MyRepo.git",
        "https://dev.azure.com/benday/", "benday", "tfvc-demo-2024", "MyRepo")]
    [DataRow(
        "https://benday.visualstudio.com/MyProject/_git/MyRepo",
        "https://benday.visualstudio.com/", "benday", "MyProject", "MyRepo")]
    [DataRow(
        "https://benday.visualstudio.com/DefaultCollection/MyProject/_git/MyRepo",
        "https://benday.visualstudio.com/DefaultCollection/", "benday", "MyProject", "MyRepo")]
    [DataRow(
        "git@ssh.dev.azure.com:v3/benday/MyProject/MyRepo",
        "https://dev.azure.com/benday/", "benday", "MyProject", "MyRepo")]
    [DataRow(
        "benday@vs-ssh.visualstudio.com:v3/benday/MyProject/MyRepo",
        "https://benday.visualstudio.com/", "benday", "MyProject", "MyRepo")]
    public void Parse_CloudUrls(
        string url,
        string expectedCollectionUrl,
        string expectedAccount,
        string expectedProject,
        string expectedRepository)
    {
        var actual = GitRemoteUrlParser.Parse(url);

        Assert.IsNotNull(actual, $"'{url}' should parse.");
        Assert.AreEqual(
            expectedCollectionUrl, actual!.CollectionUrl, $"Wrong collection url for '{url}'.");
        Assert.AreEqual(expectedAccount, actual.AccountName, $"Wrong account for '{url}'.");
        Assert.AreEqual(expectedProject, actual.ProjectName, $"Wrong project for '{url}'.");
        Assert.AreEqual(
            expectedRepository, actual.RepositoryName, $"Wrong repository for '{url}'.");
        Assert.IsTrue(actual.IsAzureDevOpsService, $"'{url}' is a cloud url.");
    }

    [TestMethod]
    [DataRow(
        "https://tfs.contoso.com/tfs/DefaultCollection/MyProject/_git/MyRepo",
        "https://tfs.contoso.com/tfs/DefaultCollection/", "DefaultCollection")]
    [DataRow(
        "https://tfs.contoso.com:8080/tfs/DefaultCollection/MyProject/_git/MyRepo",
        "https://tfs.contoso.com:8080/tfs/DefaultCollection/", "DefaultCollection")]
    [DataRow(
        "http://tfs:8080/tfs/Collection/MyProject/_git/MyRepo",
        "http://tfs:8080/tfs/Collection/", "Collection")]
    [DataRow(
        "ssh://tfs.contoso.com:22/DefaultCollection/MyProject/_git/MyRepo",
        "https://tfs.contoso.com/DefaultCollection/", "DefaultCollection")]
    public void Parse_OnPremisesUrls(
        string url, string expectedCollectionUrl, string expectedAccount)
    {
        var actual = GitRemoteUrlParser.Parse(url);

        Assert.IsNotNull(actual, $"'{url}' should parse.");
        Assert.AreEqual(
            expectedCollectionUrl, actual!.CollectionUrl, $"Wrong collection url for '{url}'.");
        Assert.AreEqual(expectedAccount, actual.AccountName, $"Wrong collection for '{url}'.");
        Assert.AreEqual("MyProject", actual.ProjectName, $"Wrong project for '{url}'.");
        Assert.AreEqual("MyRepo", actual.RepositoryName, $"Wrong repository for '{url}'.");
        Assert.IsFalse(
            actual.IsAzureDevOpsService, $"'{url}' is not a cloud url.");
    }

    [TestMethod]
    public void Parse_DecodesProjectNamesWithSpaces()
    {
        var actual = GitRemoteUrlParser.Parse(
            "https://dev.azure.com/benday/My%20Big%20Project/_git/My%20Repo");

        Assert.IsNotNull(actual, "This should parse.");
        Assert.AreEqual("My Big Project", actual!.ProjectName, "The project name should decode.");
        Assert.AreEqual("My Repo", actual.RepositoryName, "The repository name should decode.");
    }

    [TestMethod]
    public void Parse_RepositoryNameMatchingTheProjectName()
    {
        var actual = GitRemoteUrlParser.Parse(
            "https://dev.azure.com/benday/MyProject/_git/MyProject");

        Assert.IsNotNull(actual, "This should parse.");
        Assert.AreEqual("MyProject", actual!.ProjectName, "Wrong project.");
        Assert.AreEqual("MyProject", actual.RepositoryName, "Wrong repository.");
    }

    [TestMethod]
    [DataRow("https://github.com/benday-inc/azdoutil.git")]
    [DataRow("git@github.com:benday-inc/azdoutil.git")]
    [DataRow("https://gitlab.com/group/project.git")]
    [DataRow("/some/local/path")]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("not a url at all")]
    public void Parse_NonAzureDevOpsUrlsReturnNull(string url)
    {
        Assert.IsNull(
            GitRemoteUrlParser.Parse(url), $"'{url}' is not an Azure DevOps repository url.");
    }

    [TestMethod]
    public void Parse_NullReturnsNull()
    {
        Assert.IsNull(GitRemoteUrlParser.Parse(null), "Null is not a url.");
    }

    [TestMethod]
    [DataRow("https://dev.azure.com/benday/_git/MyRepo")]
    [DataRow("https://dev.azure.com/benday/MyProject/_git")]
    [DataRow("git@ssh.dev.azure.com:v3/benday/MyProject")]
    public void Parse_IncompleteUrlsReturnNull(string url)
    {
        // Missing the project, the repository, or both.  Guessing at a partial
        // url would be worse than saying it could not be read.
        Assert.IsNull(GitRemoteUrlParser.Parse(url), $"'{url}' is not usable.");
    }

    [TestMethod]
    public void Parse_KeepsTheOriginalUrl()
    {
        const string Url = "https://dev.azure.com/benday/MyProject/_git/MyRepo";

        var actual = GitRemoteUrlParser.Parse(Url);

        Assert.AreEqual(Url, actual!.OriginalUrl, "The original url should be kept.");
    }

    [TestMethod]
    public void Parse_CollectionUrlComparesAgainstAStoredConfiguration()
    {
        // A configuration's url always ends with a separator, so the parsed
        // collection url has to as well or nothing will ever match.
        var actual = GitRemoteUrlParser.Parse(
            "https://dev.azure.com/benday/MyProject/_git/MyRepo");

        Assert.IsTrue(
            actual!.CollectionUrl.EndsWith("/"), "The collection url should end with a separator.");

        var configuration = new Api.AzureDevOpsConfiguration
        {
            CollectionUrl = "https://dev.azure.com/benday"
        };

        Assert.AreEqual(
            configuration.CollectionUrl,
            actual.CollectionUrl,
            "A parsed collection url should match how a configuration stores it.");
    }
}
