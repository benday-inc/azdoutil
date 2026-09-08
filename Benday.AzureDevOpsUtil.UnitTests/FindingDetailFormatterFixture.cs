using Benday.AzureDevOpsUtil.Api.TfvcAssessment;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class FindingDetailFormatterFixture
{
    [TestMethod]
    public void FormatList_EmptyCollection_ReturnsEmptyString()
    {
        // arrange
        var items = new List<string>();

        // act
        var actual = FindingDetailFormatter.FormatList(items);

        // assert
        Assert.AreEqual(string.Empty, actual, "Should be empty.");
    }

    [TestMethod]
    public void FormatList_EverythingFits_JoinsWithoutASuffix()
    {
        // arrange
        var items = new[] { "$/Main", "$/Dev", "$/Release" };

        // act
        var actual = FindingDetailFormatter.FormatList(items);

        // assert
        Assert.AreEqual("$/Main, $/Dev, $/Release", actual, "Wrong value.");
    }

    [TestMethod]
    public void FormatList_TooLong_StaysUnderTheLimit()
    {
        // arrange
        var items = Enumerable.Range(0, 5000)
            .Select(x => $"$/Active/Dev/Application/SomeApplicationName{x}/Main")
            .ToList();

        // act
        var actual = FindingDetailFormatter.FormatList(items);

        // assert
        Assert.IsTrue(
            actual.Length <= FindingDetailFormatter.MaxDetailLength,
            $"Length {actual.Length} is over the limit of " +
                $"{FindingDetailFormatter.MaxDetailLength}.");
    }

    [TestMethod]
    public void FormatList_TooLong_SaysHowManyWereLeftOut()
    {
        // arrange
        var items = Enumerable.Range(0, 100).Select(x => new string('x', 100)).ToList();

        // act
        var actual = FindingDetailFormatter.FormatList(items, 1000);

        // assert
        StringAssert.Contains(actual, "more not shown", "Missing the omitted marker.");
        StringAssert.Contains(actual, "(100 total)", "Missing the total count.");
    }

    [TestMethod]
    public void FormatList_TooLong_CutsOnAWholeItemBoundary()
    {
        // arrange
        // Half a branch path is worse than no branch path, so every item that
        // does appear has to appear complete.
        var items = Enumerable.Range(0, 200)
            .Select(x => $"$/Active/Dev/Branch{x:0000}")
            .ToList();

        // act
        var actual = FindingDetailFormatter.FormatList(items, 500);

        // assert
        var listPart = actual.Substring(0, actual.IndexOf(", ... plus", StringComparison.Ordinal));

        foreach (var shown in listPart.Split(", "))
        {
            Assert.IsTrue(
                items.Contains(shown),
                $"'{shown}' is not a complete item from the source list.");
        }
    }

    [TestMethod]
    public void FormatList_TooLong_KeepsAsManyItemsAsFit()
    {
        // arrange
        var items = Enumerable.Range(0, 100).Select(x => $"item{x:000}").ToList();

        // act
        var actual = FindingDetailFormatter.FormatList(items, 500);

        // assert
        var listPart = actual.Substring(0, actual.IndexOf(", ... plus", StringComparison.Ordinal));
        var shownCount = listPart.Split(", ").Length;

        Assert.IsTrue(shownCount > 10, $"Only {shownCount} item(s) survived, expected more.");
        Assert.IsTrue(shownCount < 100, $"{shownCount} item(s) survived, expected a cut.");
        StringAssert.Contains(
            actual, $"plus {100 - shownCount} more not shown", "Wrong omitted count.");
    }

    [TestMethod]
    public void FormatList_SingleItemLongerThanTheLimit_IsStillClamped()
    {
        // arrange
        var items = new[] { new string('x', 5000) };

        // act
        var actual = FindingDetailFormatter.FormatList(items, 100);

        // assert
        Assert.IsTrue(actual.Length <= 100, $"Length {actual.Length} is over 100.");
        StringAssert.Contains(actual, "truncated", "Missing the truncation marker.");
    }

    [TestMethod]
    public void FormatList_NullItems_Throws()
    {
        // act & assert
        Assert.ThrowsExactly<ArgumentNullException>(
            () => FindingDetailFormatter.FormatList(null!));
    }

    [TestMethod]
    public void FormatList_MaxLengthBelowOne_Throws()
    {
        // act & assert
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => FindingDetailFormatter.FormatList(new[] { "a" }, 0));
    }

    [TestMethod]
    public void Clamp_ShortEnough_IsUnchanged()
    {
        // act
        var actual = FindingDetailFormatter.Clamp("hello", 100);

        // assert
        Assert.AreEqual("hello", actual, "Should be unchanged.");
    }

    [TestMethod]
    public void Clamp_TooLong_IsCutAndMarked()
    {
        // act
        var actual = FindingDetailFormatter.Clamp(new string('x', 500), 100);

        // assert
        Assert.AreEqual(100, actual.Length, "Wrong length.");
        StringAssert.EndsWith(actual, " ... (truncated)", "Missing the marker.");
    }

    [TestMethod]
    public void Clamp_Null_ReturnsEmptyString()
    {
        // act
        var actual = FindingDetailFormatter.Clamp(null, 100);

        // assert
        Assert.AreEqual(string.Empty, actual, "Should be empty.");
    }
}
