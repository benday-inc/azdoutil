using Benday.AzureDevOpsUtil.Api;
using Benday.AzureDevOpsUtil.Api.Commands.ProjectAdministration;
using Benday.CommandsFramework;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Benday.AzureDevOpsUtil.UnitTests;

/// <summary>
/// azdoutil used to define its own quiet mode argument and its own IsQuietMode property,
/// which hid CommandBase.IsQuietMode and answered a different question -- one read the
/// validated argument collection, the other reads the parsed command line, and they
/// disagreed on '/quiet:false'. There is now one definition, the framework's.
/// </summary>
[TestClass]
public class QuietModeFixture
{
    private static GetTeamProjectCommand GetCommand(params string[] args)
    {
        var executionInfo = new ArgumentCollectionFactory().Parse(
            new[] { "getteamproject" }.Concat(args).ToArray());

        return new GetTeamProjectCommand(
            executionInfo, new StringBuilderTextOutputProvider());
    }

    [TestMethod]
    public void QuietModeArgumentName_IsTheFrameworkReservedName()
    {
        Assert.AreEqual<string>(
            CommandFrameworkConstants.CommandArgName_QuietMode,
            Constants.ArgumentNameQuietMode);
    }

    [TestMethod]
    public void IsQuietMode_FalseWhenTheArgumentIsAbsent()
    {
        var command = GetCommand("/teamprojectname:MyProject");

        Assert.IsFalse(command.IsQuietMode);
    }

    [TestMethod]
    public void IsQuietMode_TrueForABareFlag()
    {
        var command = GetCommand("/teamprojectname:MyProject", "/quiet");

        Assert.IsTrue(command.IsQuietMode);
    }

    [TestMethod]
    public void IsQuietMode_TrueForAnExplicitTrue()
    {
        var command = GetCommand("/teamprojectname:MyProject", "/quiet:true");

        Assert.IsTrue(command.IsQuietMode);
    }

    [TestMethod]
    public void IsQuietMode_FalseForAnExplicitFalse()
    {
        // this is the case the two definitions disagreed on -- the shadowing property
        // treated any supplied value as quiet, so '/quiet:false' meant quiet
        var command = GetCommand("/teamprojectname:MyProject", "/quiet:false");

        Assert.IsFalse(command.IsQuietMode);
    }

    [TestMethod]
    public void IsQuietMode_ComesFromTheFrameworkDefinition()
    {
        // there is one property now, so reading it through the base class and through the
        // derived class cannot give different answers
        var command = GetCommand("/teamprojectname:MyProject", "/quiet");

        CommandBase asBase = command;

        Assert.AreEqual<bool>(asBase.IsQuietMode, command.IsQuietMode);
    }

    /// <summary>
    /// The two definitions answered different questions. The one that used to live on
    /// AzureDevOpsCommandBase read the validated argument collection and treated any
    /// supplied value as quiet, so '/quiet:false' meant quiet; it also could not answer at
    /// all until validation had populated the collection. The framework's reads the parsed
    /// command line, understands 'false', and is correct before the command runs.
    /// </summary>
    [TestMethod]
    public void ExplicitFalse_IsWhereTheTwoDefinitionsDisagreed()
    {
        // arrange -- reproduce what the old property looked at
        var arguments = new ArgumentCollection();

        arguments
            .AddBoolean(Constants.ArgumentNameQuietMode)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Quiet mode");

        arguments.SetValues(new Dictionary<string, string>(
            ArgumentCollection.ArgumentNameComparer)
        {
            { Constants.ArgumentNameQuietMode, "false" }
        });

        // act
        var oldAnswer =
            arguments.ContainsKey(Constants.ArgumentNameQuietMode) == true &&
            arguments[Constants.ArgumentNameQuietMode].HasValue;

        var command = GetCommand("/teamprojectname:MyProject", "/quiet:false");

        // assert
        Assert.IsTrue(oldAnswer, "the old property treated '/quiet:false' as quiet");
        Assert.IsFalse(command.IsQuietMode, "the framework's property reads it as not quiet");
    }

    [TestMethod]
    public void IsQuietMode_SetByACommandCallingAnotherCommand()
    {
        // GetCloneOfArguments() adds quiet mode for an in process call, which has to reach
        // the same property
        var executionInfo = new ArgumentCollectionFactory().Parse(
            ["getteamproject", "/teamprojectname:MyProject"]);

        var clone = executionInfo.GetCloneOfArguments("getteamproject", true);

        var command = new GetTeamProjectCommand(
            clone, new StringBuilderTextOutputProvider());

        Assert.IsTrue(command.IsQuietMode);
    }
}
