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
        var expectedUrl = "https://dev.azure.com/benday";

        var config = new AzureDevOpsConfiguration()
        {
            CollectionUrl = expectedUrl,
            Token = expectedToken,
            Name = expectedConfigurationName
        };


        AssertFileDoesNotExist(SystemUnderTest.PathToConfigurationFile);

        // act
        SystemUnderTest.Save(config);

        // assert
        AssertFileExists(SystemUnderTest.PathToConfigurationFile);

        var actual = SystemUnderTest.Get(expectedConfigurationName);

        Assert.IsNotNull(actual, $"Could not find configuration named '{expectedConfigurationName}'");
        Assert.AreEqual(expectedConfigurationName, actual.Name, "Config name was wrong");
        Assert.AreEqual(expectedUrl, actual.CollectionUrl, "Collection url was wrong");
        Assert.AreEqual(expectedToken, actual.Token, "Token was wrong");
    }

    private void AssertFileExists(string path)
    {
        Assert.IsTrue(File.Exists(path), $"File does not exist '{path}'");
    }

    private void AssertFileDoesNotExist(string path)
    {
        Assert.IsFalse(File.Exists(path), $"File should not exist '{path}'");
    }

    [TestMethod]
    public void AddConfig_Named()
    {
        // arrange
        _SystemUnderTest = Utilities.InitializeTestModeConfigurationManager();

        var expectedConfigurationName = "config123";
        var expectedToken = "token-value";
        var expectedUrl = "https://dev.azure.com/benday";

        var config = new AzureDevOpsConfiguration()
        {
            CollectionUrl = expectedUrl,
            Token = expectedToken,
            Name = expectedConfigurationName
        };

        AssertFileDoesNotExist(SystemUnderTest.PathToConfigurationFile);

        // act
        SystemUnderTest.Save(config);

        // assert
        AssertFileExists(SystemUnderTest.PathToConfigurationFile);

        var actual = SystemUnderTest.Get(expectedConfigurationName);

        Assert.IsNotNull(actual, $"Could not find configuration named '{expectedConfigurationName}'");
        Assert.AreEqual(expectedConfigurationName, actual.Name, "Config name was wrong");
        Assert.AreEqual(expectedUrl, actual.CollectionUrl, "Collection url was wrong");
        Assert.AreEqual(expectedToken, actual.Token, "Token was wrong");
    }
}
