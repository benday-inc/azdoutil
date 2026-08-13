using Benday.AzureDevOpsUtil.Api.TfvcAssessment;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class TfvcPathFixture
{
    [TestMethod]
    public void Normalize_RemovesTrailingSeparator()
    {
        Assert.AreEqual("$/App/Main", TfvcPath.Normalize("$/App/Main/"), "Trailing slash should go.");
    }

    [TestMethod]
    public void Normalize_KeepsRoot()
    {
        Assert.AreEqual("$/", TfvcPath.Normalize("$/"), "Root should survive normalization.");
    }

    [TestMethod]
    public void Normalize_ConvertsBackslashes()
    {
        Assert.AreEqual(
            "$/App/Main", TfvcPath.Normalize(@"$\App\Main"), "Backslashes should become slashes.");
    }

    [TestMethod]
    public void Normalize_EmptyBecomesRoot()
    {
        Assert.AreEqual("$/", TfvcPath.Normalize(""), "Empty should become root.");
        Assert.AreEqual("$/", TfvcPath.Normalize(null), "Null should become root.");
        Assert.AreEqual("$/", TfvcPath.Normalize("   "), "Whitespace should become root.");
    }

    [TestMethod]
    public void IsSameOrUnder_SamePath()
    {
        Assert.IsTrue(
            TfvcPath.IsSameOrUnder("$/App/Main", "$/App/Main"), "A path is under itself.");
    }

    [TestMethod]
    public void IsSameOrUnder_IgnoresCase()
    {
        Assert.IsTrue(
            TfvcPath.IsSameOrUnder("$/app/MAIN/Foo", "$/App/Main"),
            "TFVC paths are case-insensitive.");
    }

    [TestMethod]
    public void IsSameOrUnder_ChildIsUnderParent()
    {
        Assert.IsTrue(
            TfvcPath.IsSameOrUnder("$/App/Main/Source", "$/App/Main"), "Child is under parent.");
    }

    [TestMethod]
    public void IsSameOrUnder_SiblingSharingNamePrefixIsNotUnder()
    {
        // This is the comparison that a raw StartsWith gets wrong.
        Assert.IsFalse(
            TfvcPath.IsSameOrUnder("$/App/MainFrame", "$/App/Main"),
            "A sibling that shares a name prefix is not inside the folder.");
    }

    [TestMethod]
    public void IsSameOrUnder_EverythingIsUnderRoot()
    {
        Assert.IsTrue(
            TfvcPath.IsSameOrUnder("$/App/Main", "$/"), "Everything sits under the root.");
    }

    [TestMethod]
    public void IsStrictlyUnder_ExcludesSelf()
    {
        Assert.IsFalse(
            TfvcPath.IsStrictlyUnder("$/App/Main", "$/App/Main"),
            "A path is not strictly under itself.");

        Assert.IsTrue(
            TfvcPath.IsStrictlyUnder("$/App/Main/Sub", "$/App/Main"),
            "A child is strictly under its parent.");
    }

    [TestMethod]
    public void GetName_ReturnsLastSegment()
    {
        Assert.AreEqual("Main", TfvcPath.GetName("$/App/Main"), "Wrong last segment.");
        Assert.AreEqual("App", TfvcPath.GetName("$/App"), "Wrong last segment for depth 1.");
        Assert.AreEqual("$/", TfvcPath.GetName("$/"), "Root has no name segment.");
    }

    [TestMethod]
    public void GetParent_ReturnsContainingFolder()
    {
        Assert.AreEqual("$/App", TfvcPath.GetParent("$/App/Main"), "Wrong parent.");
        Assert.AreEqual("$/", TfvcPath.GetParent("$/App"), "Parent of a top folder is the root.");
        Assert.IsNull(TfvcPath.GetParent("$/"), "Root has no parent.");
    }

    [TestMethod]
    public void GetDepthBelow_CountsSegments()
    {
        Assert.AreEqual(0, TfvcPath.GetDepthBelow("$/App", "$/App"), "Same path is depth zero.");
        Assert.AreEqual(1, TfvcPath.GetDepthBelow("$/App", "$/App/Main"), "Child is depth one.");
        Assert.AreEqual(
            2, TfvcPath.GetDepthBelow("$/App", "$/App/Main/Source"), "Grandchild is depth two.");
    }

    [TestMethod]
    public void GetDepthBelow_ReturnsNegativeWhenOutsideRoot()
    {
        Assert.AreEqual(
            -1, TfvcPath.GetDepthBelow("$/App", "$/Other/Main"), "Outside the root is -1.");
    }

    [TestMethod]
    public void FindNearestEnclosing_PicksTheClosestOne()
    {
        var candidates = new[] { "$/App", "$/App/Main", "$/Other" };

        var actual = TfvcPath.FindNearestEnclosing("$/App/Main/Sub", candidates);

        Assert.AreEqual("$/App/Main", actual, "Should pick the closest enclosing path.");
    }

    [TestMethod]
    public void FindNearestEnclosing_ReturnsNullWhenNothingEncloses()
    {
        var candidates = new[] { "$/App/Main", "$/App/Dev" };

        var actual = TfvcPath.FindNearestEnclosing("$/App/Main", candidates);

        Assert.IsNull(actual, "A path does not enclose itself.");
    }
}
