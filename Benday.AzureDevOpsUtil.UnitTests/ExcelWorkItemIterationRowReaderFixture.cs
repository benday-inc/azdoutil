using Benday.AzureDevOpsUtil.Api.Excel;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class ExcelWorkItemIterationRowReaderFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
    }

    private ExcelWorkItemIterationRowReader? _SystemUnderTest;

    private ExcelWorkItemIterationRowReader SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest =
                    new ExcelWorkItemIterationRowReader(
                        new ExcelReader(
                            "C:\\Users\\benday\\OneDrive - Benjamin Day Consulting, Inc\\work-item-script.xlsx"));
            }

            return _SystemUnderTest;
        }
    }

    [TestMethod]
    public void ReadRowsAndDisplay()
    {
        // arrange

        // act
        var rows = SystemUnderTest.GetRows();

        // assert
        rows.ForEach(x => Console.WriteLine(x.ToString()));
    }
}
