using Benday.AzureDevOpsUtil.Api;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class AddUpdateConfigurationCommandFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
        _ConfigurationManager = null;
    }

    private AddUpdateConfigurationCommand? _SystemUnderTest;

    private AddUpdateConfigurationCommand SystemUnderTest
    {
        get
        {
            Assert.IsNotNull(_SystemUnderTest, "Not initialized");

            return _SystemUnderTest;
        }
    }

    private AzureDevOpsConfigurationManager? _ConfigurationManager;

    private AzureDevOpsConfigurationManager ConfigurationManager
    {
        get
        {
            if (_ConfigurationManager == null)
            {
                _ConfigurationManager =
                    Utilities.InitializeTestModeConfigurationManager();
            }

            return _ConfigurationManager;
        }
    }

    private StringBuilderTextOutputProvider? _OutputProvider;

    private StringBuilderTextOutputProvider OutputProvider
    {
        get
        {
            if (_OutputProvider == null)
            {
                _OutputProvider = new StringBuilderTextOutputProvider();
            }

            return _OutputProvider;
        }
    }

    [TestMethod]
    public void AddNamedConfiguration()
    {
        // arrange
        Utilities.AssertFileDoesNotExist(ConfigurationManager.PathToConfigurationFile);

        var expectedConfigurationName = "config123";
        var expectedToken = "token-value";
        var expectedUrl = "https://dev.azure.com/benday/";

        var commandLineArgs = Utilities.GetStringArray(
            Constants.CommandArgumentNameAddUpdateConfig,
            $"/{Constants.ArgumentNameConfigurationName}:{expectedConfigurationName}",
            $"/{Constants.ArgumentNameToken}:{expectedToken}",
            $"/{Constants.ArgumentNameCollectionUrl}:{expectedUrl}");

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new AddUpdateConfigurationCommand(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        Utilities.AssertFileExists(ConfigurationManager.PathToConfigurationFile);
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);

        var actual = ConfigurationManager.Get(expectedConfigurationName);

        Assert.IsNotNull(actual, $"Could not find configuration named '{expectedConfigurationName}'");
        Assert.AreEqual(expectedConfigurationName, actual.Name, "Config name was wrong");
        Assert.AreEqual(expectedUrl, actual.CollectionUrl, "Collection url was wrong");
        Assert.AreEqual(expectedToken, actual.Token, "Token was wrong");
        Assert.IsFalse(actual.IsWindowsAuth, "Should not be windows auth");
    }

    [TestMethod]
    public void AddNamedConfiguration_WindowsAuth()
    {
        // arrange
        Utilities.AssertFileDoesNotExist(ConfigurationManager.PathToConfigurationFile);

        var expectedConfigurationName = "config123";
        var expectedToken = string.Empty;
        var expectedUrl = "https://dev.azure.com/benday/";
        
        var commandLineArgs = Utilities.GetStringArray(
            Constants.CommandArgumentNameAddUpdateConfig,
            $"/{Constants.ArgumentNameConfigurationName}:{expectedConfigurationName}",
            $"/{Constants.ArgumentNameWindowsAuth}",
            $"/{Constants.ArgumentNameCollectionUrl}:{expectedUrl}");

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new AddUpdateConfigurationCommand(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);
        Utilities.AssertFileExists(ConfigurationManager.PathToConfigurationFile);

        var actual = ConfigurationManager.Get(expectedConfigurationName);

        Assert.IsNotNull(actual, $"Could not find configuration named '{expectedConfigurationName}'");
        Assert.AreEqual(expectedConfigurationName, actual.Name, "Config name was wrong");
        Assert.AreEqual(expectedUrl, actual.CollectionUrl, "Collection url was wrong");
        Assert.AreEqual(expectedToken, actual.Token, "Token was wrong");
        Assert.IsTrue(actual.IsWindowsAuth, "Should be windows auth");
    }

    [TestMethod]
    [ExpectedException(typeof(KnownException))]
    public void AddNamedConfiguration_CannotSetPatAndWindowsAuth()
    {
        // arrange
        Utilities.AssertFileDoesNotExist(ConfigurationManager.PathToConfigurationFile);

        var expectedConfigurationName = "config123";
        var expectedToken = string.Empty;
        var expectedUrl = "https://dev.azure.com/benday/";

        var commandLineArgs = Utilities.GetStringArray(
            Constants.CommandArgumentNameAddUpdateConfig,
            $"/{Constants.ArgumentNameConfigurationName}:{expectedConfigurationName}",
            $"/{Constants.ArgumentNameWindowsAuth}",
            $"/{Constants.ArgumentNameToken}:{expectedToken}",
            $"/{Constants.ArgumentNameCollectionUrl}:{expectedUrl}");

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new AddUpdateConfigurationCommand(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();
    }

    [TestMethod]
    [ExpectedException(typeof(KnownException))]
    public void AddNamedConfiguration_MustSetPatOrWindowsAuth()
    {
        // arrange
        Utilities.AssertFileDoesNotExist(ConfigurationManager.PathToConfigurationFile);

        var expectedConfigurationName = "config123";
        var expectedToken = string.Empty;
        var expectedUrl = "https://dev.azure.com/benday/";

        var commandLineArgs = Utilities.GetStringArray(
            Constants.CommandArgumentNameAddUpdateConfig,
            $"/{Constants.ArgumentNameConfigurationName}:{expectedConfigurationName}",
            $"/{Constants.ArgumentNameCollectionUrl}:{expectedUrl}");

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new AddUpdateConfigurationCommand(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();
    }

    [TestMethod]
    public void AddDefaultConfiguration()
    {
        // arrange
        Utilities.AssertFileDoesNotExist(ConfigurationManager.PathToConfigurationFile);

        var expectedConfigurationName = Constants.DefaultConfigurationName;
        var expectedToken = "token-value";
        var expectedUrl = "https://dev.azure.com/benday/";

        var commandLineArgs = Utilities.GetStringArray(
            Constants.CommandArgumentNameAddUpdateConfig,
            $"/{Constants.ArgumentNameToken}:{expectedToken}",
            $"/{Constants.ArgumentNameCollectionUrl}:{expectedUrl}");

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new AddUpdateConfigurationCommand(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        Utilities.AssertFileExists(ConfigurationManager.PathToConfigurationFile);
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);

        var actual = ConfigurationManager.Get(expectedConfigurationName);

        Assert.IsNotNull(actual, $"Could not find configuration named '{expectedConfigurationName}'");
        Assert.AreEqual(expectedConfigurationName, actual.Name, "Config name was wrong");
        Assert.AreEqual(expectedUrl, actual.CollectionUrl, "Collection url was wrong");
        Assert.AreEqual(expectedToken, actual.Token, "Token was wrong");
        Assert.IsFalse(actual.IsWindowsAuth, "Should not be windows auth");
    }
}
