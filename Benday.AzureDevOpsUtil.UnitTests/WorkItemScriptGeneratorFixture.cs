using Benday.AzureDevOpsUtil.Api;

namespace Benday.AzureDevOpsUtil.UnitTests;
[TestClass]
public class WorkItemScriptGeneratorFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _systemUnderTest = null;
    }

    private WorkItemScriptGenerator? _systemUnderTest;

    private WorkItemScriptGenerator SystemUnderTest
    {
        get
        {
            if (_systemUnderTest == null)
            {
                _systemUnderTest = new WorkItemScriptGenerator();
            }

            return _systemUnderTest;
        }
    }

    [TestMethod]
    public void GetRandomTitles()
    {
        for (int i = 0; i < 10; i++)
        {
            Console.WriteLine(SystemUnderTest.GetRandomTitle());
            Console.WriteLine();
        }
    }


}
