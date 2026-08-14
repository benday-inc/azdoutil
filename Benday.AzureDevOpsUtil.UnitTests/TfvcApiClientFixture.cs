using Benday.AzureDevOpsUtil.Api.TfvcAssessment;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class TfvcApiClientFixture
{
    private const string ProjectName = "tfvc-demo-2024";

    /// <summary>
    /// A real one-level listing.  The server leaves out isFolder, isBranch and
    /// size rather than sending false or null, and the listing leads with the
    /// folder that was asked for.
    /// </summary>
    private const string RealItemsPayload =
        "{\"count\":3,\"value\":[" +
        "{\"path\":\"$/tfvc-demo-2024/Main/Benday.TfvcDemo\",\"isFolder\":true," +
        "\"changeDate\":\"2025-01-22T14:38:38.54Z\",\"version\":723}," +
        "{\"path\":\"$/tfvc-demo-2024/Main/Benday.TfvcDemo/Benday.TfvcDemo.ConsoleUi\"," +
        "\"isFolder\":true,\"changeDate\":\"2025-01-22T14:38:38.54Z\",\"version\":723}," +
        "{\"path\":\"$/tfvc-demo-2024/Main/Benday.TfvcDemo/Benday.TfvcDemo.sln\"," +
        "\"size\":1490,\"changeDate\":\"2025-01-22T14:38:38.54Z\",\"version\":723}" +
        "]}";

    /// <summary>
    /// The same listing with the optional properties sent as explicit nulls.
    /// Deserializing this into non-nullable properties would throw.
    /// </summary>
    private const string ExplicitNullsPayload =
        "{\"count\":1,\"value\":[" +
        "{\"path\":\"$/App/Main/readme.txt\",\"isFolder\":null,\"isBranch\":null," +
        "\"size\":null,\"changeDate\":null}" +
        "]}";

    [TestMethod]
    public async Task GetItems_ReadsARealListing()
    {
        var client = new TfvcApiClient(url => Task.FromResult<string?>(RealItemsPayload));

        var actual = await client.GetItemsAsync(
            ProjectName, "$/tfvc-demo-2024/Main/Benday.TfvcDemo", TfvcRecursionLevel.OneLevel);

        Assert.AreEqual(3, actual.Count, "Expected three items.");

        var folder = actual[0];
        var file = actual[2];

        Assert.IsTrue(folder.IsFolder == true, "The first entry is a folder.");
        Assert.IsFalse(file.IsFolder == true, "A missing isFolder means it is not a folder.");
        Assert.AreEqual(1490, file.Size, "Wrong file size.");
        Assert.IsNull(folder.Size, "Folders carry no size.");

        // Absent isBranch means the folder is not a registered branch, which is
        // the reading the folder scan depends on.
        Assert.IsFalse(folder.IsBranch == true, "A missing isBranch is not a branch.");
    }

    [TestMethod]
    public async Task GetItems_ListingLeadsWithTheFolderThatWasAskedFor()
    {
        var client = new TfvcApiClient(url => Task.FromResult<string?>(RealItemsPayload));

        var scopePath = "$/tfvc-demo-2024/Main/Benday.TfvcDemo";

        var actual = await client.GetItemsAsync(
            ProjectName, scopePath, TfvcRecursionLevel.OneLevel);

        Assert.IsTrue(
            actual.Any(x => TfvcPath.AreEqual(x.Path, scopePath)),
            "The folder that was asked for comes back in its own listing, " +
            "so callers have to filter it out.");
    }

    [TestMethod]
    public async Task GetItems_ToleratesExplicitNulls()
    {
        var client = new TfvcApiClient(url => Task.FromResult<string?>(ExplicitNullsPayload));

        var actual = await client.GetItemsAsync(
            ProjectName, "$/App/Main", TfvcRecursionLevel.OneLevel);

        Assert.AreEqual(1, actual.Count, "Explicit nulls should not fail deserialization.");
        Assert.IsFalse(actual[0].IsFolder == true, "A null isFolder is not a folder.");
        Assert.IsNull(actual[0].Size, "A null size stays null.");
    }

    [TestMethod]
    public void ChangesetsUrl_UsesIso8601ForFromDate()
    {
        var client = new TfvcApiClient(url => Task.FromResult<string?>(null));

        var actual = client.BuildChangesetsRequestUrl(
            ProjectName,
            "$/tfvc-demo-2024/Main",
            new DateTime(2026, 5, 16, 11, 38, 29, DateTimeKind.Utc),
            10,
            0);

        // Verified against Azure DevOps: this format filters correctly.
        StringAssert.Contains(
            actual,
            "searchCriteria.fromDate=2026-05-16T11%3A38%3A29Z",
            "Wrong fromDate format.");
    }

    [TestMethod]
    public void ChangesetsUrl_OmitsFromDateWhenThereIsNone()
    {
        var client = new TfvcApiClient(url => Task.FromResult<string?>(null));

        var actual = client.BuildChangesetsRequestUrl(ProjectName, "$/App/Main", null, 10, 0);

        Assert.IsFalse(
            actual.Contains("fromDate"), "There is no date filter to apply.");
        StringAssert.Contains(actual, "$top=10", "Wrong page size.");
        StringAssert.Contains(actual, "$skip=0", "Wrong skip.");
    }
}
