using Benday.AzureDevOpsUtil.Api.SecurityMigration;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.UnitTests;

/// <summary>
/// The identity read is the one call in this module that puts unbounded data on
/// a query string, and an on-prem IIS caps that at 2048 bytes by default.
/// </summary>
[TestClass]
public class SecurityApiClientIdentityBatchFixture
{
    /// <summary>
    /// A TFS-internal group descriptor -- the longest shape that turns up in an
    /// ACL, at 95 characters once escaped.
    /// </summary>
    private static string InternalGroupDescriptor(int index) =>
        $"Microsoft.TeamFoundation.Identity;S-1-9-1551374245-1204400969-2402986413-217940861{index:D1}-0-0-0-0-1";

    private const string EmptyIdentityResponse = "{\"count\":0,\"value\":[]}";

    private static List<string> Descriptors(int count) =>
        Enumerable.Range(0, count).Select(x => InternalGroupDescriptor(x % 10)).ToList();

    [TestMethod]
    public async Task EveryRequestStaysInsideTheDefaultQueryStringLimit()
    {
        // arrange
        var requestUrls = new List<string>();

        var sut = new SecurityApiClient(
            url =>
            {
                requestUrls.Add(url);

                return Task.FromResult<string?>(EmptyIdentityResponse);
            },
            (_, _) => Task.FromResult<string?>(null));

        // act
        await sut.ReadIdentitiesByDescriptorsAsync(Descriptors(200), includeDirectMembership: true);

        // assert
        Assert.IsTrue(requestUrls.Count > 1, "200 descriptors should not fit in one request");

        foreach (var url in requestUrls)
        {
            var queryString = url[(url.IndexOf('?') + 1)..];

            Assert.IsTrue(queryString.Length <= 2048,
                $"query string was {queryString.Length} bytes, over the IIS default of 2048");
        }
    }

    [TestMethod]
    public async Task EveryDescriptorIsAsked_ForAcrossTheBatches()
    {
        // arrange
        var requestUrls = new List<string>();

        var sut = new SecurityApiClient(
            url =>
            {
                requestUrls.Add(url);

                return Task.FromResult<string?>(EmptyIdentityResponse);
            },
            (_, _) => Task.FromResult<string?>(null));

        var descriptors = Descriptors(60);

        // act
        await sut.ReadIdentitiesByDescriptorsAsync(descriptors, includeDirectMembership: true);

        // assert
        var everythingSent = string.Join(" ", requestUrls);

        foreach (var descriptor in descriptors.Distinct())
        {
            Assert.IsTrue(everythingSent.Contains(Uri.EscapeDataString(descriptor)),
                $"'{descriptor}' was never asked for");
        }
    }

    /// <summary>
    /// A dropped identity batch used to read as "this machine holds no
    /// permission grants", which on a migration is a wrong answer that looks
    /// like a right one.
    /// </summary>
    [TestMethod]
    public async Task AFailedBatchIsReportedRatherThanSwallowed()
    {
        // arrange
        var sut = new SecurityApiClient(
            _ => Task.FromResult<string?>(null),
            (_, _) => Task.FromResult<string?>(null));

        // act & assert
        var actual = await Assert.ThrowsExactlyAsync<KnownException>(
            () => sut.ReadIdentitiesByDescriptorsAsync(
                Descriptors(5), includeDirectMembership: true));

        Assert.IsTrue(actual.Message.Contains("maxQueryString"),
            "the message should name the most likely cause");
    }

    [TestMethod]
    public async Task ASingleOverlongDescriptorStillGetsItsOwnRequest()
    {
        // arrange -- one descriptor longer than the whole budget
        var requestUrls = new List<string>();

        var sut = new SecurityApiClient(
            url =>
            {
                requestUrls.Add(url);

                return Task.FromResult<string?>(EmptyIdentityResponse);
            },
            (_, _) => Task.FromResult<string?>(null));

        var descriptors = new List<string>
        {
            new string('a', SecurityApiClient.MaxQueryStringLength + 100),
            InternalGroupDescriptor(1)
        };

        // act
        await sut.ReadIdentitiesByDescriptorsAsync(descriptors, includeDirectMembership: true);

        // assert
        Assert.AreEqual<int>(2, requestUrls.Count,
            "the overlong descriptor should not drag the next one down with it");
    }
}
