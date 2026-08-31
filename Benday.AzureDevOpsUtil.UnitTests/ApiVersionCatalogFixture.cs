using Benday.AzureDevOpsUtil.Api.ApiVersioning;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class ApiVersionCatalogFixture
{
    /// <summary>
    /// Shaped like what an Azure DevOps Server 2019 collection returns for
    /// OPTIONS on its _apis root: a 5.x wave, with one resource that never left
    /// preview and so reports a released version of "0.0".
    /// </summary>
    private const string CatalogJson = """
    {
      "count": 5,
      "value": [
        { "id": "603fe2ac", "area": "core", "resourceName": "projects",
          "routeTemplate": "_apis/{resource}/{*projectId}",
          "resourceVersion": 4, "minVersion": "1.0", "maxVersion": "5.1", "releasedVersion": "5.0" },
        { "id": "225f7195", "area": "git", "resourceName": "repositories",
          "routeTemplate": "{project}/_apis/{area}/{resource}/{repositoryId}",
          "resourceVersion": 1, "minVersion": "1.0", "maxVersion": "5.1", "releasedVersion": "5.0" },
        { "id": "d5b216de", "area": "git", "resourceName": "branchStats",
          "routeTemplate": "{project}/_apis/{area}/repositories/{repositoryId}/stats/branches",
          "resourceVersion": 1, "minVersion": "1.0", "maxVersion": "5.1", "releasedVersion": "5.0" },
        { "id": "083c4d89", "area": "distributedtask", "resourceName": "deploymentgroups",
          "routeTemplate": "{project}/_apis/{area}/{resource}/{deploymentGroupId}",
          "resourceVersion": 1, "minVersion": "3.0", "maxVersion": "5.1", "releasedVersion": "0.0" },
        { "id": "7ab4e64e", "area": "wit", "resourceName": "workitems",
          "routeTemplate": "{project}/_apis/{area}/{resource}/{id}",
          "resourceVersion": 3, "minVersion": "1.0", "maxVersion": "5.1", "releasedVersion": "5.0" }
      ]
    }
    """;

    private ApiVersionCatalog Catalog
    {
        get
        {
            var catalog = ApiVersionCatalog.Parse(CatalogJson);

            Assert.IsNotNull(catalog, "catalog should have parsed");

            return catalog;
        }
    }

    private static ApiVersion Version(string value)
    {
        Assert.IsTrue(ApiVersion.TryParse(value, out var version), $"'{value}' should parse");

        return version;
    }

    [TestMethod]
    public void Parse_ReadsAllLocations()
    {
        // arrange & act
        var actual = Catalog;

        // assert
        Assert.AreEqual<int>(5, actual.Locations.Count, "location count");
        Assert.AreEqual<string>("5.0", actual.MaxReleasedVersion.ToString(), "wave version");
        Assert.AreEqual<string>("5.1", actual.MaxVersion.ToString(), "preview ceiling");
    }

    [TestMethod]
    public void Parse_ReturnsNullForNonJson()
    {
        // arrange -- an on-prem server that wants a sign-in answers with html
        var html = "<html><head><title>Sign In</title></head></html>";

        // act
        var actual = ApiVersionCatalog.Parse(html);

        // assert
        Assert.IsNull(actual, "html is not a catalog");
    }

    [TestMethod]
    public void FindLocation_CollectionScopedUrl()
    {
        // arrange & act
        var actual = Catalog.FindLocation("/DefaultCollection/_apis/projects?$top=10000&api-version=7.0");

        // assert
        Assert.IsNotNull(actual, "should have matched");
        Assert.AreEqual<string>("projects", actual.ResourceName, "resource");
    }

    [TestMethod]
    public void FindLocation_ProjectScopedUrl()
    {
        // arrange & act
        var actual = Catalog.FindLocation("/DefaultCollection/GnarlyCorp/_apis/wit/workitems/42?api-version=7.0");

        // assert
        Assert.IsNotNull(actual, "should have matched");
        Assert.AreEqual<string>("workitems", actual.ResourceName, "resource");
    }

    /// <summary>
    /// Both the repositories template and the branch stats template can be made
    /// to match once their id segments are optional, and only one of them
    /// describes this url.
    /// </summary>
    [TestMethod]
    public void FindLocation_PrefersTheMoreSpecificTemplate()
    {
        // arrange & act
        var actual = Catalog.FindLocation(
            "/DefaultCollection/_apis/git/repositories/9f3c/stats/branches?api-version=7.1");

        // assert
        Assert.IsNotNull(actual, "should have matched");
        Assert.AreEqual<string>("branchStats", actual.ResourceName, "resource");
    }

    [TestMethod]
    public void FindLocation_UnknownPath()
    {
        // arrange & act
        var actual = Catalog.FindLocation("/DefaultCollection/_apis/nonesuch/widgets");

        // assert
        Assert.IsNull(actual, "nothing in the catalog describes this");
    }

    [TestMethod]
    public void TryResolve_ClampsToTheReleasedVersion()
    {
        // arrange & act
        var changed = Catalog.TryResolve("/_apis/projects", Version("7.0"), out var actual);

        // assert
        Assert.IsTrue(changed, "7.0 is newer than this collection");
        Assert.AreEqual<string>("5.0", actual.ToString(), "clamped");
    }

    [TestMethod]
    public void TryResolve_LeavesASupportedVersionAlone()
    {
        // arrange & act
        var changed = Catalog.TryResolve("/_apis/projects", Version("4.1"), out var actual);

        // assert
        Assert.IsFalse(changed, "4.1 is within range and should be sent as written");
        Assert.AreEqual<string>("4.1", actual.ToString(), "unchanged");
    }

    [TestMethod]
    public void TryResolve_PreviewRequestKeepsItsSuffix()
    {
        // arrange & act
        var changed = Catalog.TryResolve(
            "/_apis/distributedtask/deploymentgroups", Version("7.1-preview.1"), out var actual);

        // assert
        Assert.IsTrue(changed, "7.1-preview.1 is newer than this collection");
        Assert.AreEqual<string>("5.1-preview.1", actual.ToString(), "clamped, still preview");
    }

    /// <summary>
    /// A resource that never shipped out of preview reports "0.0" as its
    /// released version, and clamping to that would produce a request for
    /// nothing at all.
    /// </summary>
    [TestMethod]
    public void TryResolve_PreviewOnlyResourceFallsBackToItsMaxVersion()
    {
        // arrange & act
        var changed = Catalog.TryResolve(
            "/_apis/distributedtask/deploymentgroups", Version("7.0"), out var actual);

        // assert
        Assert.IsTrue(changed, "7.0 is newer than this collection");
        Assert.AreEqual<string>("5.1", actual.ToString(), "clamped to the preview ceiling, not to 0.0");
    }

    /// <summary>
    /// An unrecognised path still gets clamped -- the collection's overall wave
    /// is a better guess than sending a version it has never heard of.
    /// </summary>
    [TestMethod]
    public void TryResolve_UnknownPathUsesTheCollectionWave()
    {
        // arrange & act
        var changed = Catalog.TryResolve("/_apis/nonesuch/widgets", Version("7.0"), out var actual);

        // assert
        Assert.IsTrue(changed, "should still clamp");
        Assert.AreEqual<string>("5.0", actual.ToString(), "clamped to the wave");
    }
}
