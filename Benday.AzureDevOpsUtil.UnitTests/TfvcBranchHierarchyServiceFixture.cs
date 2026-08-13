using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.AzureDevOpsUtil.Api.TfvcAssessment;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class TfvcBranchHierarchyServiceFixture
{
    private TfvcBranchHierarchyService SystemUnderTest => new();

    private static TfvcBranchInfo Branch(string path, params TfvcBranchInfo[] children)
    {
        return new TfvcBranchInfo
        {
            Path = path,
            Children = children.ToList()
        };
    }

    [TestMethod]
    public void Build_SimpleMainDevReleaseTree()
    {
        var branches = new List<TfvcBranchInfo>
        {
            Branch("$/App/Main",
                Branch("$/App/Dev"),
                Branch("$/App/Release"))
        };

        var actual = SystemUnderTest.Build(branches, "$/App");

        Assert.AreEqual(1, actual.Roots.Count, "Expected one root branch.");
        Assert.AreEqual("$/App/Main", actual.Roots[0].Path, "Wrong root path.");
        Assert.AreEqual(2, actual.Roots[0].Children.Count, "Expected two children of Main.");
        Assert.AreEqual(3, actual.AllPaths.Count, "Expected three branches in total.");
        Assert.AreEqual(0, actual.NestedBranches.Count, "Siblings are not nested branches.");
    }

    [TestMethod]
    public void Build_EmptyResult()
    {
        var actual = SystemUnderTest.Build(new List<TfvcBranchInfo>(), "$/App");

        Assert.AreEqual(0, actual.Roots.Count, "Expected no roots.");
        Assert.AreEqual(0, actual.AllPaths.Count, "Expected no branch paths.");
        Assert.AreEqual(0, actual.NestedBranches.Count, "Expected no nested branches.");
    }

    [TestMethod]
    public void Build_NullResult()
    {
        var actual = SystemUnderTest.Build(null, "$/App");

        Assert.AreEqual(0, actual.Roots.Count, "A null payload should produce no roots.");
    }

    [TestMethod]
    public void Build_DeepHierarchy()
    {
        var branches = new List<TfvcBranchInfo>
        {
            Branch("$/App/Main",
                Branch("$/App/Dev",
                    Branch("$/App/Feature1",
                        Branch("$/App/Feature1a"))))
        };

        var actual = SystemUnderTest.Build(branches, "$/App");

        Assert.AreEqual(4, actual.AllPaths.Count, "Expected four branches.");
        Assert.AreEqual(1, actual.Roots.Count, "Expected a single root.");

        var level1 = actual.Roots[0].Children.Single();
        var level2 = level1.Children.Single();
        var level3 = level2.Children.Single();

        Assert.AreEqual("$/App/Dev", level1.Path, "Wrong level 1 path.");
        Assert.AreEqual("$/App/Feature1", level2.Path, "Wrong level 2 path.");
        Assert.AreEqual("$/App/Feature1a", level3.Path, "Wrong level 3 path.");
        Assert.AreEqual(0, actual.NestedBranches.Count, "None of these are nested by path.");
    }

    [TestMethod]
    public void Build_ScopesToSuppliedPath()
    {
        var branches = new List<TfvcBranchInfo>
        {
            Branch("$/App/Main", Branch("$/App/Dev")),
            Branch("$/Other/Main")
        };

        var actual = SystemUnderTest.Build(branches, "$/App");

        Assert.AreEqual(2, actual.AllPaths.Count, "Branches outside the scope should be dropped.");
        Assert.IsFalse(
            actual.AllPaths.Contains("$/Other/Main"), "Out of scope branch leaked into results.");
    }

    [TestMethod]
    public void Build_BranchWhoseParentIsOutOfScopeBecomesARoot()
    {
        var branches = new List<TfvcBranchInfo>
        {
            Branch("$/Shared/Main", Branch("$/App/Dev"))
        };

        var actual = SystemUnderTest.Build(branches, "$/App");

        Assert.AreEqual(1, actual.Roots.Count, "Expected the in-scope child to become a root.");
        Assert.AreEqual("$/App/Dev", actual.Roots[0].Path, "Wrong root path.");
    }

    [TestMethod]
    public void Build_SkipsDeletedBranches()
    {
        var deleted = Branch("$/App/Old");
        deleted.IsDeleted = true;

        var branches = new List<TfvcBranchInfo>
        {
            Branch("$/App/Main"),
            deleted
        };

        var actual = SystemUnderTest.Build(branches, "$/App");

        Assert.AreEqual(1, actual.AllPaths.Count, "Deleted branches should not be counted.");
        Assert.AreEqual("$/App/Main", actual.AllPaths[0], "Wrong surviving branch.");
    }

    [TestMethod]
    public void Build_NestedBranchProducesOneFinding()
    {
        var branches = new List<TfvcBranchInfo>
        {
            Branch("$/App/Main",
                Branch("$/App/Main/Feature"))
        };

        var actual = SystemUnderTest.Build(branches, "$/App");

        Assert.AreEqual(1, actual.NestedBranches.Count, "Expected exactly one nested branch pair.");
        Assert.AreEqual(
            "$/App/Main/Feature", actual.NestedBranches[0].ChildPath, "Wrong nested child path.");
        Assert.AreEqual(
            "$/App/Main", actual.NestedBranches[0].ParentPath, "Wrong enclosing branch path.");
    }

    [TestMethod]
    public void Build_NestedThreeDeepReportsNearestEnclosingOnly()
    {
        var branches = new List<TfvcBranchInfo>
        {
            Branch("$/App/Main",
                Branch("$/App/Main/Feature",
                    Branch("$/App/Main/Feature/Sub")))
        };

        var actual = SystemUnderTest.Build(branches, "$/App");

        Assert.AreEqual(2, actual.NestedBranches.Count, "Expected two pairs, not three.");

        var sub = actual.NestedBranches.Single(x => x.ChildPath == "$/App/Main/Feature/Sub");

        Assert.AreEqual(
            "$/App/Main/Feature", sub.ParentPath, "Should report the nearest enclosing branch.");
    }

    [TestMethod]
    public void Build_SimilarlyNamedSiblingIsNotNested()
    {
        var branches = new List<TfvcBranchInfo>
        {
            Branch("$/App/Main"),
            Branch("$/App/MainFrame")
        };

        var actual = SystemUnderTest.Build(branches, "$/App");

        Assert.AreEqual(
            0, actual.NestedBranches.Count, "A name prefix is not folder containment.");
    }
}
