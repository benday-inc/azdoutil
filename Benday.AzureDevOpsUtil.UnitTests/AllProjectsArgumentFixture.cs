using Benday.AzureDevOpsUtil.Api;
using Benday.AzureDevOpsUtil.Api.Commands.Builds;
using Benday.CommandsFramework;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Benday.AzureDevOpsUtil.UnitTests;

/// <summary>
/// The 'scan one project or all of them' commands used to decide which they were doing by
/// asking Arguments.HasValue() for the all-projects flag. An argument configured with
/// WithDefaultValue() always has a value, so the check was always true: running the command
/// with no arguments at all fell through to the single-project path with an empty project
/// name and called an url with no project in it, which the server answered with a 404.
/// </summary>
[TestClass]
public class AllProjectsArgumentFixture
{
    [TestMethod]
    public void AllProjectsArgument_HasValueIsTrueEvenWhenNotSupplied()
    {
        // this is the framework behavior that the commands used to depend on being false
        var command = new FindNuGetToolInstallerCommand(
            new ArgumentCollectionFactory().Parse(
                new[] { Constants.CommandName_FindNuGetToolInstaller }),
            new StringBuilderTextOutputProvider());

        var arguments = command.GetArguments();

        Assert.IsTrue(
            arguments[Constants.ArgumentNameAllProjects].HasValue,
            "A defaulted argument reports HasValue == true, so HasValue() cannot be used " +
            "to find out whether the flag was supplied.");
    }

    [TestMethod]
    public async Task FindNuGetToolInstaller_NoArgumentsThrowsKnownException()
    {
        await AssertNoArgumentsThrowsKnownException(
            Constants.CommandName_FindNuGetToolInstaller,
            (info, output) => new FindNuGetToolInstallerCommand(info, output));
    }

    [TestMethod]
    public async Task FindDeploymentGroupUsages_NoArgumentsThrowsKnownException()
    {
        await AssertNoArgumentsThrowsKnownException(
            Constants.CommandName_FindDeploymentGroupUsages,
            (info, output) => new FindDeploymentGroupUsagesCommand(info, output));
    }

    [TestMethod]
    public async Task FindDemands_NoArgumentsThrowsKnownException()
    {
        await AssertNoArgumentsThrowsKnownException(
            Constants.CommandName_FindDemands,
            (info, output) => new FindDemandsCommand(info, output));
    }

    private static async Task AssertNoArgumentsThrowsKnownException(
        string commandName,
        Func<CommandExecutionInfo, ITextOutputProvider, Command> createCommand)
    {
        var command = createCommand(
            new ArgumentCollectionFactory().Parse(new[] { commandName }),
            new StringBuilderTextOutputProvider());

        var exception = await Assert.ThrowsExactlyAsync<KnownException>(
            async () => await command.ExecuteAsync(CancellationToken.None));

        Assert.IsTrue(
            exception.Message.Contains(Constants.ArgumentNameAllProjects),
            $"Message should name the all-projects argument. Message was '{exception.Message}'.");
    }
}
