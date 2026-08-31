using Benday.AzureDevOpsUtil.Api.ApiVersioning;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class ServerVersionReaderFixture
{
    /// <summary>
    /// Shaped like the page context every Azure DevOps page embeds, with the
    /// build number an on-prem About page also shows.  The asset version is
    /// there because the real page carries several version-looking numbers and
    /// the build number has to be picked out from among them.
    /// </summary>
    private const string OnPremAboutPage =
        "<html><head><script>var x = {\"webContext\":{\"isHosted\":false," +
        "\"serviceVersion\":\"Dev17.M143.4 (AzureDevOpsServer_20190305.4)\"," +
        "\"sessionId\":\"7121edfa-74d2-4c6c-b32e-bd92ffe747d6\"}};</script>" +
        "<link href=\"/_static/tfs/M143/_scripts/common.js?version=10.25136.1\" /></head>" +
        "<body><div class=\"about-page\">Azure DevOps Server 2019<br/>" +
        "Version 17.143.28621.4</div></body></html>";

    private const string HostedAboutPage =
        "<html><head><script>var x = {\"webContext\":{\"isHosted\":true," +
        "\"serviceVersion\":\"Dev20.M277.1 (AzureDevOps_M277_20260814.1)\"}};</script>" +
        "</head><body>Azure DevOps Services</body></html>";

    [TestMethod]
    public void ReadsTheServiceVersionAndBuildNumberFromAnOnPremPage()
    {
        // arrange & act
        var actual = ServerVersionReader.Read(OnPremAboutPage);

        // assert
        Assert.AreEqual<string>("Dev17.M143.4 (AzureDevOpsServer_20190305.4)",
            actual.ServiceVersion, "service version");
        Assert.AreEqual<string>("17.143.28621.4", actual.BuildNumber, "build number");
    }

    /// <summary>
    /// The hosted service is continuously deployed and carries no assembly
    /// version, so only the service version comes back.
    /// </summary>
    [TestMethod]
    public void ReadsWhatIsThereOnAHostedPage()
    {
        // arrange & act
        var actual = ServerVersionReader.Read(HostedAboutPage);

        // assert
        Assert.AreEqual<string>("Dev20.M277.1 (AzureDevOps_M277_20260814.1)",
            actual.ServiceVersion, "service version");
        Assert.AreEqual<string>(string.Empty, actual.BuildNumber, "no build number to find");
        Assert.IsFalse(actual.IsEmpty, "something was read");
    }

    [TestMethod]
    public void AssetVersionsAreNotMistakenForTheBuildNumber()
    {
        // arrange -- a page with a cache-busting asset version but no build
        var html = "<html><head><link href=\"/x.js?version=10.25136.1\" />" +
            "<script>var x = {\"foo\":\"1.2.3.4\"};</script></head></html>";

        // act
        var actual = ServerVersionReader.Read(html);

        // assert
        Assert.AreEqual<string>(string.Empty, actual.BuildNumber,
            "neither of those is a server build number");
    }

    [TestMethod]
    public void AnUnreadablePageIsEmptyRatherThanAnError()
    {
        // arrange & act
        var actual = ServerVersionReader.Read("<html><body>Please sign in</body></html>");

        // assert
        Assert.IsTrue(actual.IsEmpty, "nothing to report");
    }

    [TestMethod]
    public void HandlesNoPageAtAll()
    {
        // arrange & act
        var actual = ServerVersionReader.Read(null);

        // assert
        Assert.IsTrue(actual.IsEmpty, "nothing to report");
    }

    [TestMethod]
    public void MapsTheCustomerBuildToItsRelease()
    {
        // arrange & act
        var actual = AzureDevOpsProductVersion.DescribeBuild("17.143.28621.4");

        // assert
        Assert.AreEqual<string>("Azure DevOps Server 2019 (RTW line)", actual, "release");
    }

    /// <summary>
    /// The second octet is the update train, which the api-version cannot see:
    /// both of these are "5.0" servers.
    /// </summary>
    [TestMethod]
    public void DistinguishesUpdateTrainsWithinOneRelease()
    {
        // arrange & act
        var rtw = AzureDevOpsProductVersion.DescribeBuild("17.143.28621.4");
        var updateOne = AzureDevOpsProductVersion.DescribeBuild("17.153.29207.5");

        // assert
        Assert.AreNotEqual<string>(rtw, updateOne, "these are different releases");
        Assert.AreEqual<string>("Azure DevOps Server 2019 Update 1 or later", updateOne, "update 1");
    }

    [TestMethod]
    public void UnknownBuildsDescribeToNothing()
    {
        // arrange & act
        var actual = AzureDevOpsProductVersion.DescribeBuild("banana");

        // assert
        Assert.AreEqual<string>(string.Empty, actual, "not a build number");
    }
}
