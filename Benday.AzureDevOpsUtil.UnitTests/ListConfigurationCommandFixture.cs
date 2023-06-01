using Benday.AzureDevOpsUtil.Api;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class ListConfigurationCommandFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
        _ConfigurationManager = null;
    }

    private ListConfigurationCommand? _SystemUnderTest;

    private ListConfigurationCommand SystemUnderTest
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
    public void ListNamedConfiguration_ThatExists()
    {
        // arrange
        Utilities.AssertFileDoesNotExist(ConfigurationManager.PathToConfigurationFile);

        AzureDevOpsConfiguration expected0 = new AzureDevOpsConfiguration()
        {
            Name = "config1",
            CollectionUrl = "https://dev.azure.com/benday",
            Token = "token1"
        };
        ConfigurationManager.Save(expected0);

        AzureDevOpsConfiguration expected1 = new AzureDevOpsConfiguration()
        {
            Name = "config2",
            CollectionUrl = "https://azdo2022.benday.com/DefaultCollection",
            Token = "token2",
            IsWindowsAuth = true
        };
        ConfigurationManager.Save(expected1);

        Utilities.AssertFileExists(ConfigurationManager.PathToConfigurationFile);

        var commandLineArgs = Utilities.GetStringArray(
            Constants.CommandArgumentNameListConfig,
            $"/{Constants.ArgumentNameConfigurationName}:config2");

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new ListConfigurationCommand(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        Utilities.AssertFileExists(ConfigurationManager.PathToConfigurationFile);
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);

        Assert.IsTrue(output.Contains("Token: token2"), "didn't find token2 in output");
        Assert.IsTrue(output.Contains($"Collection Url: {expected1.CollectionUrl}"), "didn't find url2 in output");
        Assert.IsTrue(output.Contains($"Account Name / TPC Name: {expected1.AccountNameOrCollectionName}"), "didn't find account name or TPC name in output");
        Assert.IsTrue(output.Contains("Name: config2"), "didn't find config2 in output");
        Assert.IsTrue(output.Contains($"Use Windows Auth: {true}"), "didn't find use windows auth in output");
    }

    [TestMethod]
    public void ListAllConfigs()
    {
        // arrange
        Utilities.AssertFileDoesNotExist(ConfigurationManager.PathToConfigurationFile);

        ConfigurationManager.Save(new AzureDevOpsConfiguration()
        {
            Name = "config1",
            CollectionUrl = "https://dev.azure.com/benday",
            Token = "token1"
        });

        AzureDevOpsConfiguration expected1 = new AzureDevOpsConfiguration()
        {
            Name = "config2",
            CollectionUrl = "https://azdo2022.benday.com/DefaultCollection",
            Token = "token2"
        };

        ConfigurationManager.Save(expected1);

        Utilities.AssertFileExists(ConfigurationManager.PathToConfigurationFile);

        var commandLineArgs = Utilities.GetStringArray(
            Constants.CommandArgumentNameListConfig);

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new ListConfigurationCommand(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        Utilities.AssertFileExists(ConfigurationManager.PathToConfigurationFile);
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);

        Assert.IsTrue(output.Contains("Token: token2"), "didn't find token2 in output");
        Assert.IsTrue(output.Contains($"Collection Url: {expected1.CollectionUrl}"), "didn't find url2 in output");
        Assert.IsTrue(output.Contains($"Account Name / TPC Name: {expected1.AccountNameOrCollectionName}"), "didn't find account name or TPC name in output");
        Assert.IsTrue(output.Contains("Name: config2"), "didn't find config2 in output");
    }

    [TestMethod]
    public void ListAllConfigs_NoConfigs()
    {
        // arrange
        Utilities.AssertFileDoesNotExist(ConfigurationManager.PathToConfigurationFile);

        Assert.AreEqual<int>(0, ConfigurationManager.GetAll().Length, "There should not be any configs at start of test.");

        var commandLineArgs = Utilities.GetStringArray(
            Constants.CommandArgumentNameListConfig);

        var executionInfo = new ArgumentCollectionFactory().Parse(commandLineArgs);

        _SystemUnderTest = new ListConfigurationCommand(executionInfo, OutputProvider);

        // act
        _SystemUnderTest.Execute();

        // assert        
        var output = OutputProvider.GetOutput();
        Console.WriteLine(output);

        Assert.IsTrue(output.Contains("No configurations"), "didn't find message in output");
    }
}
