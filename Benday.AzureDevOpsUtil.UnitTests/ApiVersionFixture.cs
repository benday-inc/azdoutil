using Benday.AzureDevOpsUtil.Api.ApiVersioning;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class ApiVersionFixture
{
    [TestMethod]
    public void Parse_ReleasedVersion()
    {
        // arrange & act
        var parsed = ApiVersion.TryParse("7.0", out var actual);

        // assert
        Assert.IsTrue(parsed, "should have parsed");
        Assert.AreEqual<int>(7, actual.Major, "major");
        Assert.AreEqual<int>(0, actual.Minor, "minor");
        Assert.IsFalse(actual.IsPreview, "should not be preview");
        Assert.AreEqual<string>("7.0", actual.ToString(), "round trip");
    }

    [TestMethod]
    public void Parse_NumberedPreview()
    {
        // arrange & act
        var parsed = ApiVersion.TryParse("5.0-preview.1", out var actual);

        // assert
        Assert.IsTrue(parsed, "should have parsed");
        Assert.IsTrue(actual.IsPreview, "should be preview");
        Assert.AreEqual<int>(1, actual.PreviewNumber, "preview number");
        Assert.AreEqual<string>("5.0-preview.1", actual.ToString(), "round trip");
    }

    [TestMethod]
    public void Parse_UnnumberedPreview()
    {
        // arrange & act
        var parsed = ApiVersion.TryParse("5.0-preview", out var actual);

        // assert
        Assert.IsTrue(parsed, "should have parsed");
        Assert.IsTrue(actual.IsPreview, "should be preview");
        Assert.AreEqual<string>("5.0-preview", actual.ToString(), "round trip");
    }

    [TestMethod]
    public void Parse_MajorOnly()
    {
        // arrange & act
        var parsed = ApiVersion.TryParse("6", out var actual);

        // assert
        Assert.IsTrue(parsed, "should have parsed");
        Assert.AreEqual<string>("6.0", actual.ToString(), "minor defaults to zero");
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("banana")]
    [DataRow("7.0.1")]
    [DataRow("7.0-beta")]
    [DataRow("7.0-preview.x")]
    public void Parse_RejectsNonVersions(string value)
    {
        // arrange & act
        var parsed = ApiVersion.TryParse(value, out _);

        // assert
        Assert.IsFalse(parsed, $"'{value}' is not an api-version");
    }

    /// <summary>
    /// The whole point of parsing rather than string-comparing: "10.0" sorts
    /// ahead of "7.0" as text, and a clamp using that ordering would raise the
    /// version instead of lowering it.
    /// </summary>
    [TestMethod]
    public void Ordering_IsNumericNotAlphabetic()
    {
        // arrange
        ApiVersion.TryParse("10.0", out var ten);
        ApiVersion.TryParse("7.0", out var seven);

        // act & assert
        Assert.IsTrue(ten > seven, "10.0 is newer than 7.0");
    }

    [TestMethod]
    public void Ordering_ReleasedOutranksItsOwnPreviews()
    {
        // arrange
        ApiVersion.TryParse("7.1", out var released);
        ApiVersion.TryParse("7.1-preview.2", out var preview);

        // act & assert
        Assert.IsTrue(released > preview, "7.1 shipped after 7.1-preview.2");
    }

    [TestMethod]
    public void Ordering_HigherPreviewNumberIsNewer()
    {
        // arrange
        ApiVersion.TryParse("7.1-preview.2", out var two);
        ApiVersion.TryParse("7.1-preview.1", out var one);

        // act & assert
        Assert.IsTrue(two > one, "preview.2 is newer than preview.1");
    }

    /// <summary>
    /// A command that asked for a preview resource still wants the preview
    /// resource on an older server, so the suffix survives the renumbering.
    /// </summary>
    [TestMethod]
    public void WithNumberOf_KeepsPreviewSuffix()
    {
        // arrange
        ApiVersion.TryParse("7.1-preview.1", out var requested);
        ApiVersion.TryParse("5.0", out var target);

        // act
        var actual = requested.WithNumberOf(target);

        // assert
        Assert.AreEqual<string>("5.0-preview.1", actual.ToString(), "renumbered");
    }

    [TestMethod]
    public void WithNumberOf_ReleasedStaysReleased()
    {
        // arrange
        ApiVersion.TryParse("7.0", out var requested);
        ApiVersion.TryParse("5.0", out var target);

        // act
        var actual = requested.WithNumberOf(target);

        // assert
        Assert.AreEqual<string>("5.0", actual.ToString(), "renumbered");
    }
}
