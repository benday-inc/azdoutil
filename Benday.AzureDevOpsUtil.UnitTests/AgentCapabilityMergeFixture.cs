using Benday.AzureDevOpsUtil.Api.AgentCapabilities;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class AgentCapabilityMergeFixture
{
    private static Dictionary<string, string> Dict(params (string Key, string Value)[] pairs)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in pairs)
        {
            result[pair.Key] = pair.Value;
        }

        return result;
    }

    [TestMethod]
    public void Merge_KeepsExistingAndAddsIncoming()
    {
        // arrange
        var existing = Dict(("A", "1"), ("B", "2"));
        var incoming = Dict(("C", "3"));

        // act
        var result = AgentCapabilityMerge.ComputeFinal(existing, incoming, replace: false);

        // assert
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual("1", result["A"]);
        Assert.AreEqual("2", result["B"]);
        Assert.AreEqual("3", result["C"]);
    }

    [TestMethod]
    public void Merge_IncomingOverridesExistingValue()
    {
        // arrange
        var existing = Dict(("A", "1"));
        var incoming = Dict(("A", "99"));

        // act
        var result = AgentCapabilityMerge.ComputeFinal(existing, incoming, replace: false);

        // assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("99", result["A"]);
    }

    [TestMethod]
    public void Merge_OverrideIsCaseInsensitiveOnKey()
    {
        // arrange
        var existing = Dict(("VisualStudio", "2019"));
        var incoming = Dict(("visualstudio", "2022"));

        // act
        var result = AgentCapabilityMerge.ComputeFinal(existing, incoming, replace: false);

        // assert
        Assert.AreEqual(1, result.Count, "the differing-case key should not create a second entry");
        Assert.AreEqual("2022", result.Values.Single());
    }

    [TestMethod]
    public void Replace_ReturnsOnlyIncoming()
    {
        // arrange
        var existing = Dict(("A", "1"), ("B", "2"));
        var incoming = Dict(("C", "3"));

        // act
        var result = AgentCapabilityMerge.ComputeFinal(existing, incoming, replace: true);

        // assert
        Assert.AreEqual(1, result.Count, "replace drops anything not named in incoming");
        Assert.AreEqual("3", result["C"]);
        Assert.IsFalse(result.ContainsKey("A"));
    }
}
