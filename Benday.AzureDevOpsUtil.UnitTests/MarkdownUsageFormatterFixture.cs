using Benday.AzureDevOpsUtil.Api.UsageFormatters;
using Benday.CommandsFramework;
namespace Benday.AzureDevOpsUtil.UnitTests;
[TestClass]
public class MarkdownUsageFormatterFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
    }

    private MarkdownUsageFormatter? _SystemUnderTest;

    private MarkdownUsageFormatter SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest = new MarkdownUsageFormatter();
            }

            return _SystemUnderTest;
        }
    }

    [TestMethod]
    public void FormatUsagesAsMarkdown()
    {
        // arrange
        var assembly = typeof(StringUtility).Assembly;

        var usages = new CommandAttributeUtility().GetAllCommandUsages(assembly);

        // act
        var actual = SystemUnderTest.Format(usages);

        // assert
        Assert.IsNotNull(actual, "actual markdown was null");
        Assert.IsFalse(string.IsNullOrEmpty(actual), "actual markdown was empty");

        Console.WriteLine($"{actual}");
    }
}
