using Microsoft.VisualStudio.TestTools.UnitTesting;
using Benday.WorkItemUtility.Api;
using System;

namespace Benday.WorkItemUtility.UnitTests;

[TestClass]
public class ExcelWorkItemScriptRowReaderFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
    }

    private ExcelWorkItemScriptRowReader? _SystemUnderTest;

    private ExcelWorkItemScriptRowReader SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest =
                    new ExcelWorkItemScriptRowReader(
                        new CodeGenerator.Excel.ExcelReader(
                            "C:\\Users\\benday\\OneDrive - Benjamin Day Consulting, Inc\\work-item-script2.xlsx"));
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
