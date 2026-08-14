using Benday.AzureDevOpsUtil.Api.AgentCapabilities;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class AgentCapabilityServiceFixture
{
    private FakeAgentPoolClient _client = null!;
    private AgentCapabilityService _sut = null!;

    [TestInitialize]
    public void OnTestInitialize()
    {
        _client = new FakeAgentPoolClient();
        _sut = new AgentCapabilityService(_client);
    }

    private void SeedTwoPools()
    {
        _client.AddPool(1, "Default");
        _client.AddPool(2, "Linux");

        _client.AddAgent(1, "Default", 10, "BUILD01", ("VisualStudio", "2022"));
        _client.AddAgent(1, "Default", 11, "BUILD02");
        _client.AddAgent(2, "Linux", 20, "LINUX01", ("docker", "true"));
    }

    [TestMethod]
    public async Task GetInventory_ReturnsAllAgentsOrderedByPoolThenName()
    {
        // arrange
        SeedTwoPools();

        // act
        var inventory = await _sut.GetInventoryAsync();

        // assert
        Assert.AreEqual(3, inventory.Count);
        Assert.AreEqual("BUILD01", inventory[0].AgentName, "Default pool sorts before Linux");
        Assert.AreEqual("BUILD02", inventory[1].AgentName);
        Assert.AreEqual("LINUX01", inventory[2].AgentName);
    }

    [TestMethod]
    public async Task GetInventory_PoolFilter_LimitsToOnePool()
    {
        // arrange
        SeedTwoPools();

        // act
        var inventory = await _sut.GetInventoryAsync("Linux");

        // assert
        Assert.AreEqual(1, inventory.Count);
        Assert.AreEqual("LINUX01", inventory[0].AgentName);
    }

    [TestMethod]
    public async Task BuildExport_ExcludesAgentsWithoutUserCapabilities()
    {
        // arrange
        SeedTwoPools();
        var inventory = await _sut.GetInventoryAsync();

        // act
        var export = AgentCapabilityService.BuildExport("https://server/tfs/col", inventory);

        // assert
        Assert.AreEqual("https://server/tfs/col", export.CollectionUrl);
        Assert.AreEqual(2, export.Agents.Count, "BUILD02 has no user capabilities and is left out");
        CollectionAssert.AreEquivalent(
            new[] { "BUILD01", "LINUX01" },
            export.Agents.Select(x => x.AgentName).ToArray());
    }

    [TestMethod]
    public async Task PlanSet_Merge_FlagsOnlyRealChanges()
    {
        // arrange
        SeedTwoPools();
        var inventory = await _sut.GetInventoryAsync();
        var incoming = new Dictionary<string, string> { ["VisualStudio"] = "2022" };

        // act - apply VisualStudio=2022 to every agent, merging
        var plan = _sut.PlanSet(inventory, incoming, replace: false);

        // assert
        var build01 = plan.Single(x => x.AgentName == "BUILD01");
        Assert.IsFalse(build01.WillChange, "BUILD01 already has VisualStudio=2022");

        var build02 = plan.Single(x => x.AgentName == "BUILD02");
        Assert.IsTrue(build02.WillChange, "BUILD02 gains a new capability");
        CollectionAssert.AreEqual(new[] { "VisualStudio" }, build02.AddedKeys.ToArray());
    }

    [TestMethod]
    public async Task PlanSet_Replace_RemovesUnnamedCapabilities()
    {
        // arrange
        SeedTwoPools();
        var inventory = await _sut.GetInventoryAsync();
        var incoming = new Dictionary<string, string> { ["docker"] = "true" };

        // act - replace on LINUX01 which already has docker=true only
        var plan = _sut.PlanSet(
            inventory.Where(x => x.AgentName == "LINUX01"), incoming, replace: true);

        var linux = plan.Single();

        // assert
        Assert.IsFalse(linux.WillChange, "replace with the identical single capability is a no-op");
    }

    [TestMethod]
    public async Task PlanSet_Replace_DropsExtraKeys()
    {
        // arrange
        _client.AddPool(1, "Default");
        _client.AddAgent(1, "Default", 10, "BUILD01", ("Keep", "1"), ("Drop", "2"));
        var inventory = await _sut.GetInventoryAsync();
        var incoming = new Dictionary<string, string> { ["Keep"] = "1" };

        // act
        var plan = _sut.PlanSet(inventory, incoming, replace: true);
        var item = plan.Single();

        // assert
        Assert.IsTrue(item.WillChange);
        CollectionAssert.AreEqual(new[] { "Drop" }, item.RemovedKeys.ToArray());
    }

    [TestMethod]
    public async Task ApplyAsync_PutsFinalCapabilitiesForAgent()
    {
        // arrange
        SeedTwoPools();
        var inventory = await _sut.GetInventoryAsync();
        var incoming = new Dictionary<string, string> { ["SpecialSoftware"] = "yes" };
        var plan = _sut.PlanSet(
            inventory.Where(x => x.AgentName == "BUILD01"), incoming, replace: false);

        // act
        await _sut.ApplyAsync(plan.Single());

        // assert
        Assert.AreEqual(1, _client.Updates.Count);
        var update = _client.Updates.Single();
        Assert.AreEqual(1, update.PoolId);
        Assert.AreEqual(10, update.AgentId);
        Assert.AreEqual("2022", update.Capabilities["VisualStudio"], "merge preserved the existing capability");
        Assert.AreEqual("yes", update.Capabilities["SpecialSoftware"], "and added the new one");
    }

    [TestMethod]
    public async Task PlanImport_MatchesByName_AndReportsUnmatched()
    {
        // arrange
        SeedTwoPools();
        var inventory = await _sut.GetInventoryAsync();

        var export = new AgentCapabilityExport
        {
            Agents =
            {
                new AgentCapabilityRecord
                {
                    AgentName = "BUILD02",
                    PoolName = "Default",
                    UserCapabilities = { ["Java"] = "17" }
                },
                new AgentCapabilityRecord
                {
                    AgentName = "GHOST",
                    PoolName = "Default",
                    UserCapabilities = { ["Nothing"] = "here" }
                }
            }
        };

        // act
        var plan = _sut.PlanImport(inventory, export, replace: false);

        // assert
        Assert.AreEqual(1, plan.Matched.Count);
        Assert.AreEqual("BUILD02", plan.Matched[0].AgentName);
        Assert.AreEqual(11, plan.Matched[0].AgentId, "matched to the live agent id, not the exported one");
        Assert.IsTrue(plan.Matched[0].WillChange);

        Assert.AreEqual(1, plan.Unmatched.Count);
        Assert.AreEqual("GHOST", plan.Unmatched[0].AgentName);
    }

    [TestMethod]
    public async Task PlanImport_DuplicateAgentName_DisambiguatesByPool()
    {
        // arrange - same agent name in two pools
        _client.AddPool(1, "PoolA");
        _client.AddPool(2, "PoolB");
        _client.AddAgent(1, "PoolA", 10, "AGENT");
        _client.AddAgent(2, "PoolB", 20, "AGENT");
        var inventory = await _sut.GetInventoryAsync();

        var export = new AgentCapabilityExport
        {
            Agents =
            {
                new AgentCapabilityRecord
                {
                    AgentName = "AGENT",
                    PoolName = "PoolB",
                    UserCapabilities = { ["X"] = "1" }
                }
            }
        };

        // act
        var plan = _sut.PlanImport(inventory, export, replace: false);

        // assert
        Assert.AreEqual(1, plan.Matched.Count);
        Assert.AreEqual(20, plan.Matched[0].AgentId, "should pick the AGENT in PoolB");
    }
}
