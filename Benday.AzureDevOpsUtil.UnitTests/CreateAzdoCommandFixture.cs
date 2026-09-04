using Benday.AzureDevOpsUtil.Api;
using Benday.AzureDevOpsUtil.Api.Commands.ProjectAdministration;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.UnitTests;

/// <summary>
/// A command that runs another command used to build the arguments for it by cloning its own
/// command line and then deleting the arguments that did not belong (RemoveAllArgumentsExcept,
/// deleted along with these tests' predecessor). That meant a value reached the command it was
/// aimed at only when both commands spelled the argument the same way, and only when it had
/// actually been typed -- a default value declared by the calling command was never part of the
/// command line, so it never travelled. These cover what replaced it.
/// </summary>
[TestClass]
public class CreateAzdoCommandFixture
{
    private static TestParentCommand CreateParent(params string[] args)
    {
        var argv = new List<string> { "unittest-parent" };
        argv.AddRange(args);

        var executionInfo = new ArgumentCollectionFactory().Parse(argv.ToArray());

        // matches what Program.cs configures. The command being run inherits these options,
        // so an argument it does not declare is a validation failure rather than something
        // it quietly ignores -- which is the check TheCommandBeingRunValidates() is making.
        executionInfo.Options = new DefaultProgramOptions
        {
            StrictArgumentValidation = true
        };

        var command = new TestParentCommand(
            executionInfo,
            new StringBuilderTextOutputProvider());

        // applies the command line on top of the argument definitions, which is what running
        // the command would do before OnExecute() gets to ask for a value
        command.ValidateArguments();

        return command;
    }

    [TestMethod]
    public void PassesTheConfigurationNameToTheCommandBeingRun()
    {
        var parent = CreateParent("--config", "on-prem", "--teamproject", "MyProject");

        var child = parent.CreateChild();

        Assert.AreEqual<string>(
            "on-prem",
            child.ExecutionInfo.Arguments[Constants.ArgumentNameConfigurationName],
            "The connection the calling command is using has to travel to the command it runs.");
    }

    [TestMethod]
    public void PassesTheDefaultConfigurationNameWhenNoneWasSupplied()
    {
        var parent = CreateParent("--teamproject", "MyProject");

        var child = parent.CreateChild();

        Assert.AreEqual<string>(
            Constants.DefaultConfigurationName,
            child.ExecutionInfo.Arguments[Constants.ArgumentNameConfigurationName],
            "No configuration name means the default one, not an empty one.");
    }

    [TestMethod]
    public void PassesAValueThatCameFromTheCallingCommandsDefault()
    {
        // 'processname' is not on the command line at all: it is the calling command's
        // declared default. Cloning the command line lost it and createproject was handed
        // an empty process template name.
        var parent = CreateParent("--teamproject", "MyProject");

        var child = parent.CreateChild();

        Assert.AreEqual<string>(
            TestParentCommand.DefaultProcessTemplateName,
            child.ExecutionInfo.Arguments[Constants.CommandArg_ProcessTemplateName],
            "A defaulted value should reach the command being run.");
    }

    [TestMethod]
    public void DoesNotPassArgumentsTheOtherCommandDidNotAskFor()
    {
        var parent = CreateParent(
            "--teamproject", "MyProject", "--sprintcount", "6");

        var child = parent.CreateChild();

        Assert.IsFalse(
            child.ExecutionInfo.Arguments.ContainsKey(Constants.CommandArg_SprintCount),
            "The command being run should only get the arguments it was given by name.");
    }

    [TestMethod]
    public void TheCommandBeingRunValidates()
    {
        var parent = CreateParent("--teamproject", "MyProject");

        var child = parent.CreateChild();

        var failures = child.ValidateArguments();

        Assert.AreEqual<int>(
            0, failures.Count,
            "createproject should have everything it requires and nothing it did not ask " +
            "for. " + string.Join(" ", failures.Select(x => x.Message)));
    }

    [TestMethod]
    public void TheCommandBeingRunRunsQuietly()
    {
        var parent = CreateParent("--teamproject", "MyProject");

        var child = parent.CreateChild();

        Assert.AreEqual<string>(
            "true",
            child.ExecutionInfo.Arguments[CommandFrameworkConstants.CommandArgName_QuietMode],
            "A command run from another command should not write over its caller's output.");
    }

    /// <summary>
    /// Stands in for a command that creates a team project as part of a larger job -- the
    /// data generator commands do exactly this. Not registered as a real command: it lives
    /// in the test assembly, which nothing reflects over.
    /// </summary>
    private class TestParentCommand : AzureDevOpsCommandBase
    {
        public const string DefaultProcessTemplateName = "Scrum";

        public TestParentCommand(
            CommandExecutionInfo info, ITextOutputProvider outputProvider) :
            base(info, outputProvider)
        {
        }

        public override ArgumentCollection GetArguments()
        {
            var arguments = new ArgumentCollection();

            AddCommonArguments(arguments);

            arguments.AddString(Constants.CommandArg_TeamProjectName)
                .AsNotRequired()
                .WithDescription("Name of the team project");

            arguments.AddString(Constants.CommandArg_ProcessTemplateName)
                .AsNotRequired()
                .WithDefaultValue(DefaultProcessTemplateName)
                .WithDescription("Process template name");

            arguments.AddInt32(Constants.CommandArg_SprintCount)
                .AsNotRequired()
                .WithDefaultValue(4)
                .WithDescription("Number of sprints to generate");

            return arguments;
        }

        protected override Task OnExecute(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public CreateTeamProjectCommand CreateChild()
        {
            return CreateAzdoCommand<CreateTeamProjectCommand>(args => args
                .Set(Constants.ArgumentNameTeamProjectName,
                    Arguments.GetStringValue(Constants.CommandArg_TeamProjectName))
                .Set(Constants.CommandArg_ProcessTemplateName,
                    Arguments.GetStringValue(Constants.CommandArg_ProcessTemplateName)));
        }
    }
}
