using Benday.AzureDevOpsUtil.Api.ApiVersioning;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class ApiVersionRequestRewriterFixture
{
    private static ServerApiVersionInfo ServerAt(string maximum)
    {
        Assert.IsTrue(ApiVersion.TryParse(maximum, out var version), "maximum should parse");

        return ServerApiVersionInfo.FromReportedMaximum(version);
    }

    [TestMethod]
    public void RewritesTheVersionAndNothingElse()
    {
        // arrange
        var url = "https://server/tfs/DefaultCollection/_apis/projects?$top=10000&api-version=7.0";

        // act
        var actual = ApiVersionRequestRewriter.Rewrite(url, ServerAt("5.1"));

        // assert
        Assert.AreEqual<string>(
            "https://server/tfs/DefaultCollection/_apis/projects?$top=10000&api-version=5.1",
            actual, "rewritten");
    }

    [TestMethod]
    public void KeepsParametersThatFollowTheVersion()
    {
        // arrange
        var url = "https://server/_apis/build/definitions?api-version=7.1&name=Nightly";

        // act
        var actual = ApiVersionRequestRewriter.Rewrite(url, ServerAt("5.1"));

        // assert
        Assert.AreEqual<string>(
            "https://server/_apis/build/definitions?api-version=5.1&name=Nightly",
            actual, "rewritten");
    }

    [TestMethod]
    public void LeavesASupportedVersionAlone()
    {
        // arrange
        var url = "https://dev.azure.com/acct/_apis/projects?api-version=7.0";

        // act
        var actual = ApiVersionRequestRewriter.Rewrite(url, ServerAt("7.2"));

        // assert
        Assert.IsNull(actual, "nothing to change on a current collection");
    }

    [TestMethod]
    public void LeavesAUrlWithNoVersionAlone()
    {
        // arrange
        var url = "https://server/_apis/connectionData";

        // act
        var actual = ApiVersionRequestRewriter.Rewrite(url, ServerAt("5.1"));

        // assert
        Assert.IsNull(actual, "no api-version to lower");
    }

    /// <summary>
    /// Only the query parameter is a version; the same text in the path is part
    /// of the address.
    /// </summary>
    [TestMethod]
    public void IgnoresTheWordInThePath()
    {
        // arrange
        var url = "https://server/_apis/git/repositories/api-version=7.0/items";

        // act
        var actual = ApiVersionRequestRewriter.Rewrite(url, ServerAt("5.1"));

        // assert
        Assert.IsNull(actual, "that is a path segment, not a parameter");
    }
}
