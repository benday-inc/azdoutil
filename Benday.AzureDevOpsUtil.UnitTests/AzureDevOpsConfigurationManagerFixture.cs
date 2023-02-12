using Benday.AzureDevOpsUtil.Api;
namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class AzureDevOpsConfigurationManagerFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
    }

    private AzureDevOpsConfigurationManager _SystemUnderTest;

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
}