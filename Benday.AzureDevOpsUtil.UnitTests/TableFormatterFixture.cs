using System;

using Benday.AzureDevOpsUtil.Api.DataFormatting;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class TableFormatterFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
    }

    private TableFormatter? _SystemUnderTest;

    private TableFormatter SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest = new TableFormatter();
            }

            return _SystemUnderTest;
        }
    }

    [TestMethod]
    public void MultiColumn_PopulatesColumnWidths()
    {
        // arrange
        SystemUnderTest.AddColumn("Last Name");
        SystemUnderTest.AddColumn("First Name");
        SystemUnderTest.AddColumn("Email Address");

        var data = GetTestData();

        var longestLastName = data.Max(x => x.LastName.Length);
        var longestFirstName = data.Max(x => x.FirstName.Length);
        var longestEmailAddress = data.Max(x => x.EmailAddress.Length);

        // act
        foreach (var item in data)
        {
            SystemUnderTest.AddData(item.LastName, item.FirstName, item.EmailAddress);
        }

        // assert
        Assert.AreEqual(longestLastName, SystemUnderTest.Columns[0].WidthOfLongestValue);
        Assert.AreEqual(longestFirstName, SystemUnderTest.Columns[1].WidthOfLongestValue);
        Assert.AreEqual(longestEmailAddress, SystemUnderTest.Columns[2].WidthOfLongestValue);
    }

    [TestMethod]
    public void FormatTable()
    {
        // arrange
        SystemUnderTest.AddColumn("Last Name");
        SystemUnderTest.AddColumn("First Name");
        SystemUnderTest.AddColumn("Email Address");

        var data = GetTestData();

        var expectedLineLength = 50;

        var longestLastName = data.Max(x => x.LastName.Length);
        var longestFirstName = data.Max(x => x.FirstName.Length);
        var longestEmailAddress = data.Max(x => x.EmailAddress.Length);
        
        // act
        foreach (var item in data)
        {
            SystemUnderTest.AddData(item.LastName, item.FirstName, item.EmailAddress);
        }

        // assert
        string result = SystemUnderTest.FormatTable();

        var lines = result.Split(Environment.NewLine);

        if (lines.Length == 0)
        {
            Assert.Fail("No lines in result.");
        }
        else if (lines.Length == 1)
        {
            Assert.Fail("Only header row in result.");
        }
        else
        {
            var lastLine = lines[lines.Length - 1];

            if (string.IsNullOrWhiteSpace(lastLine) == true)
            {
                // it's ok for the last line to be empty
                // remove the last line if it's empty
                var temp = lines.ToList();
                temp.RemoveAt(temp.Count - 1);
                lines = temp.ToArray();
            }
        }

        Assert.AreEqual(data.Count + 1, lines.Length, "Line count is wrong.");

        var actualRowLength = SystemUnderTest.Columns.Sum(x => x.Width) + SystemUnderTest.Columns.Count - 1;

        Assert.AreEqual(expectedLineLength, actualRowLength, "Row length is wrong.");

        var expectedHeaderRow = $"{SystemUnderTest.Columns[0].NamePadded} {SystemUnderTest.Columns[1].NamePadded} {SystemUnderTest.Columns[2].NamePadded}";
        Assert.AreEqual(expectedHeaderRow, lines[0], "Header row is wrong.");
        Assert.AreEqual(expectedLineLength, lines[0].Length, $"Line 0 length is wrong.");

        var lineNumber = 0;

        foreach (var line in lines)
        {
            if (lineNumber == 0)
            {
                // skip the header row
                lineNumber++;
                continue;
            }

            Assert.AreEqual(expectedLineLength, line.Length, $"Line {lineNumber} length is wrong.");
            lineNumber++;
        }



        for (int index = 0; index < data.Count; index++)
        {
            var item = data[index];
            var line = lines[index + 1];

            var paddedLastName = item.LastName.PadRight(SystemUnderTest.Columns[0].Width);
            var paddedFirstName = item.FirstName.PadRight(SystemUnderTest.Columns[1].Width);
            var paddedEmailAddress = item.EmailAddress.PadRight(SystemUnderTest.Columns[2].Width);

            var expectedLine = $"{paddedLastName} {paddedFirstName} {paddedEmailAddress}";

            Assert.AreEqual(expectedLine, line, $"Line {index} is wrong.");
        }
    }

    private List<Person> GetTestData()
    {
        var returnValues = new List<Person>();

        returnValues.Add(new Person()
        {
            LastName = "Smith",
            FirstName = "John",
            EmailAddress = "john.smith@email.com"
        });

        returnValues.Add(new Person()
        {
            LastName = "Jones",
            FirstName = "Sally",
            EmailAddress = "sally.jones@emailthingy.org"
        });

        returnValues.Add(new Person()
        {
            LastName = "Subramanian",
            FirstName = "Rajesh",
            EmailAddress = "rj@stuff.net"
        });

        returnValues.Add(new Person()
        {
            LastName = "Ng",
            FirstName = "Paul",
            EmailAddress = "rj@stuff.net"
        });

        return returnValues;
    }

    private class Person
    {
        public string LastName { get; set; } = String.Empty;
        public string FirstName { get; set; } = String.Empty;
        public string EmailAddress { get; set; } = String.Empty;
    }
}