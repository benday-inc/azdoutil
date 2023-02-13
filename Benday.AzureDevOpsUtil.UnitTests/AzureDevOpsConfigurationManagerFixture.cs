using Benday.AzureDevOpsUtil.Api;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class AzureDevOpsConfigurationManagerFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
    }

    private AzureDevOpsConfigurationManager? _SystemUnderTest;

    private AzureDevOpsConfigurationManager SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest = new AzureDevOpsConfigurationManager();
            }

            return _SystemUnderTest;
        }
    }

    [TestMethod]
    public void GetConfigurationFilePath()
    {
        // arrange
        var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var expected = Path.Combine(userProfilePath, Constants.ExeName, Constants.ConfigFileName);

        // act
        var actual = SystemUnderTest.PathToConfigurationFile;

        // assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(actual), "Path should not be empty");

        Console.WriteLine($"{actual}");

        Assert.AreEqual<string>(expected, actual, "Path was wrong");
    }

    [TestMethod]
    public void AddConfig_Default()
    {
        // arrange
        _SystemUnderTest = Utilities.InitializeTestModeConfigurationManager();

        var expectedConfigurationName = Constants.DefaultConfigurationName;
        var expectedToken = "token-value";
        var expectedUrl = "https://dev.azure.com/benday/";

        var config = new AzureDevOpsConfiguration()
        {
            CollectionUrl = expectedUrl,
            Token = expectedToken,
            Name = expectedConfigurationName
        };


        Utilities.AssertFileDoesNotExist(SystemUnderTest.PathToConfigurationFile);

        // act
        SystemUnderTest.Save(config);

        // assert
        Utilities.AssertFileExists(SystemUnderTest.PathToConfigurationFile);

        var actual = SystemUnderTest.Get(expectedConfigurationName);

        Assert.IsNotNull(actual, $"Could not find configuration named '{expectedConfigurationName}'");
        Assert.AreEqual(expectedConfigurationName, actual.Name, "Config name was wrong");
        Assert.AreEqual(expectedUrl, actual.CollectionUrl, "Collection url was wrong");
        Assert.AreEqual(expectedToken, actual.Token, "Token was wrong");
    }    

    [TestMethod]
    public void AddConfig_Named()
    {
        // arrange
        _SystemUnderTest = Utilities.InitializeTestModeConfigurationManager();

        var expectedConfigurationName = "config123";
        var expectedToken = "token-value";
        var expectedUrl = "https://dev.azure.com/benday/";

        var config = new AzureDevOpsConfiguration()
        {
            CollectionUrl = expectedUrl,
            Token = expectedToken,
            Name = expectedConfigurationName
        };

        Utilities.AssertFileDoesNotExist(SystemUnderTest.PathToConfigurationFile);

        // act
        SystemUnderTest.Save(config);

        // assert
        Utilities.AssertFileExists(SystemUnderTest.PathToConfigurationFile);

        var actual = SystemUnderTest.Get(expectedConfigurationName);

        Assert.IsNotNull(actual, $"Could not find configuration named '{expectedConfigurationName}'");
        Assert.AreEqual(expectedConfigurationName, actual.Name, "Config name was wrong");
        Assert.AreEqual(expectedUrl, actual.CollectionUrl, "Collection url was wrong");
        Assert.AreEqual(expectedToken, actual.Token, "Token was wrong");
    }

    [TestMethod]
    public void RemoveConfig()
    {
        // arrange
        _SystemUnderTest = Utilities.InitializeTestModeConfigurationManager();

        SystemUnderTest.Save(new AzureDevOpsConfiguration()
        {
            Name = "config1",
            CollectionUrl = "url1",
            Token = "token1"
        });

        SystemUnderTest.Save(new AzureDevOpsConfiguration()
        {
            Name = "config2",
            CollectionUrl = "url2",
            Token = "token2"
        });

        // act
        SystemUnderTest.Remove("config2");

        // assert
        var actual = SystemUnderTest.Get("config2");

        Assert.IsNull(actual, $"Should not find configuration named 'config2'");
    }

    [TestMethod]
    public void GetAll()
    {
        // arrange
        _SystemUnderTest = Utilities.InitializeTestModeConfigurationManager();

        SystemUnderTest.Save(new AzureDevOpsConfiguration()
        {
            Name = "config1",
            CollectionUrl = "url1",
            Token = "token1"
        });

        SystemUnderTest.Save(new AzureDevOpsConfiguration()
        {
            Name = "config2",
            CollectionUrl = "url2",
            Token = "token2"
        });

        // act
        var actual = SystemUnderTest.GetAll();

        // assert
        Assert.IsNotNull(actual);
        Assert.AreEqual<int>(2, actual.Length, "Config count was wrong.");
    }
}
