using Benday.AzureDevOpsUtil.Api;
namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class AzureDevOpsConfigurationFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
    }

    private AzureDevOpsConfiguration? _SystemUnderTest;

    private AzureDevOpsConfiguration SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest = new AzureDevOpsConfiguration();
            }

            return _SystemUnderTest;
        }
    }

    [TestMethod]
    public void UrlAddsTrailingForwardSlash()
    {
        // arrange
        var url = "https://dev.azure.com/benday";
        var expected = $"{url}/";

        SystemUnderTest.CollectionUrl = url;

        // act
        var actual = SystemUnderTest.CollectionUrl;

        // assert
        Assert.AreEqual<string>(expected, actual, "Collection url is wrong.");
    }

    [TestMethod]
    public void UrlPreservesTrailingSlash()
    {
        // arrange
        var url = "https://dev.azure.com/benday/";
        var expected = $"{url}";

        SystemUnderTest.CollectionUrl = url;

        // act
        var actual = SystemUnderTest.CollectionUrl;

        // assert
        Assert.AreEqual<string>(expected, actual, "Collection url is wrong.");
    }

    [TestMethod]
    public void IsAzureDevOpsService_ForServiceCollectionUrl()
    {
        // arrange
        var url = "https://dev.azure.com/benday/";
        var expected = true;

        SystemUnderTest.CollectionUrl = url;

        // act
        var actual = SystemUnderTest.IsAzureDevOpsService;

        // assert
        Assert.AreEqual<bool>(expected, actual, "IsAzureDevOpsService is wrong.");
    }

    [TestMethod]
    public void IsAzureDevOpsService_ForServerCollectionUrl()
    {
        // arrange
        var url = "https://azdo2022.benday.com/DefaultCollection/";
        var expected = false;

        SystemUnderTest.CollectionUrl = url;

        // act
        var actual = SystemUnderTest.IsAzureDevOpsService;

        // assert
        Assert.AreEqual<bool>(expected, actual, "IsAzureDevOpsService is wrong.");
    }
}