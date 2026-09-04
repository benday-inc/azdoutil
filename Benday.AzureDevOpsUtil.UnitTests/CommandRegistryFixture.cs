using Benday.AzureDevOpsUtil.Api;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.UnitTests;

/// <summary>
/// Turns the framework's own registry checks into part of the suite. Building
/// the registry throws on genuinely ambiguous names (duplicates), and reports
/// everything that makes a command unreachable -- including a name or alias
/// that collides with a reserved keyword like 'tui' or 'completion', which the
/// framework matches before it ever asks the registry. A command with that
/// kind of collision builds clean and silently stops running, so this test is
/// the only detector.
/// </summary>
[TestClass]
public class CommandRegistryFixture
{
    /// <summary>
    /// Mirrors what Program.cs configures, so the registry under test is the
    /// one the tool actually builds at run time.
    /// </summary>
    private static DefaultProgramOptions GetProgramOptions()
    {
        return new DefaultProgramOptions
        {
            ApplicationName = "Azure DevOps Utilities",
            Website = "https://www.benday.com",
            StrictArgumentValidation = true
        };
    }

    [TestMethod]
    public void CommandsHaveNoRegistryProblems()
    {
        // act
        var registry = CommandRegistry.Build(
            GetProgramOptions(), typeof(AzureDevOpsCommandBase).Assembly);

        // assert -- print them, because "collection had 3 items" does not say which
        foreach (var problem in registry.Problems)
        {
            Console.WriteLine(problem);
        }

        Assert.AreEqual<int>(0, registry.Problems.Count,
            "every command and alias should be reachable");
    }

    [TestMethod]
    public void CommandsHaveNoArgumentProblems()
    {
        // arrange
        var utility = new CommandAttributeUtility(GetProgramOptions());

        // act
        var problems = utility.GetArgumentProblems(typeof(AzureDevOpsCommandBase).Assembly);

        // assert
        foreach (var problem in problems)
        {
            Console.WriteLine(problem);
        }

        Assert.AreEqual<int>(0, problems.Count,
            "every command's argument definitions should be valid");
    }
}
