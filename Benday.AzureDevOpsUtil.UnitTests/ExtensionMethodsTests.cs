using Benday.CommandsFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class ExtensionMethodsTests
{
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void RemoveAllArgumentsExcept_ThrowsArgumentNullException_WhenExecInfoIsNull()
    {
        // Arrange
        CommandExecutionInfo? execInfo = null;

        // Act
        execInfo!.RemoveAllArgumentsExcept(false);

        // Assert - ExpectedException
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void RemoveAllArgumentsExcept_ThrowsArgumentNullException_WhenArgumentsIsNull()
    {
        // Arrange
        var execInfo = new CommandExecutionInfo
        {
            Arguments = null
        };

        // Act
        execInfo.RemoveAllArgumentsExcept(false);

        // Assert - ExpectedException
    }

    [TestMethod]
    public void RemoveAllArgumentsExcept_RemovesAllArguments_WhenNoArgumentsToKeep()
    {
        // Arrange
        var execInfo = new CommandExecutionInfo
        {
            Arguments = new Dictionary<string, string>
            {
                { "arg1", "value1" },
                { "arg2", "value2" },
                { "arg3", "value3" }
            }
        };

        // Act
        execInfo.RemoveAllArgumentsExcept(false);

        // Assert
        Assert.AreEqual(0, execInfo.Arguments.Count);
    }

    [TestMethod]
    public void RemoveAllArgumentsExcept_PreservesCommonArguments_WhenPreserveCommonArgumentsIsTrue()
    {
        // Arrange
        var execInfo = new CommandExecutionInfo
        {
            Arguments = new Dictionary<string, string>
            {
                { Constants.ArgumentNameQuietMode, "true" },
                { Constants.ArgumentNameConfigurationName, "config" },
                { "arg1", "value1" },
                { "arg2", "value2" }
            }
        };

        // Act
        execInfo.RemoveAllArgumentsExcept(true);

        // Assert
        Assert.AreEqual(2, execInfo.Arguments.Count);
        Assert.IsTrue(execInfo.Arguments.ContainsKey(Constants.ArgumentNameQuietMode));
        Assert.IsTrue(execInfo.Arguments.ContainsKey(Constants.ArgumentNameConfigurationName));
    }

    [TestMethod]
    public void RemoveAllArgumentsExcept_PreservesSpecifiedArguments_WhenArgumentNamesToKeepProvided()
    {
        // Arrange
        var execInfo = new CommandExecutionInfo
        {
            Arguments = new Dictionary<string, string>
            {
                { "arg1", "value1" },
                { "arg2", "value2" },
                { "arg3", "value3" }
            }
        };

        // Act
        execInfo.RemoveAllArgumentsExcept(false, "arg1", "arg3");

        // Assert
        Assert.AreEqual(2, execInfo.Arguments.Count);
        Assert.IsTrue(execInfo.Arguments.ContainsKey("arg1"));
        Assert.IsTrue(execInfo.Arguments.ContainsKey("arg3"));
        Assert.IsFalse(execInfo.Arguments.ContainsKey("arg2"));
    }

    [TestMethod]
    public void RemoveAllArgumentsExcept_PreservesCommonAndSpecifiedArguments_WhenBothProvided()
    {
        // Arrange
        var execInfo = new CommandExecutionInfo
        {
            Arguments = new Dictionary<string, string>
            {
                { Constants.ArgumentNameQuietMode, "true" },
                { Constants.ArgumentNameConfigurationName, "config" },
                { "arg1", "value1" },
                { "arg2", "value2" },
                { "arg3", "value3" }
            }
        };

        // Act
        execInfo.RemoveAllArgumentsExcept(true, "arg2");

        // Assert
        Assert.AreEqual(3, execInfo.Arguments.Count);
        Assert.IsTrue(execInfo.Arguments.ContainsKey(Constants.ArgumentNameQuietMode));
        Assert.IsTrue(execInfo.Arguments.ContainsKey(Constants.ArgumentNameConfigurationName));
        Assert.IsTrue(execInfo.Arguments.ContainsKey("arg2"));
        Assert.IsFalse(execInfo.Arguments.ContainsKey("arg1"));
        Assert.IsFalse(execInfo.Arguments.ContainsKey("arg3"));
    }

    [TestMethod]
    public void RemoveAllArgumentsExcept_IsCaseInsensitive_WhenCheckingArgumentNames()
    {
        // Arrange
        var execInfo = new CommandExecutionInfo
        {
            Arguments = new Dictionary<string, string>
            {
                { "ARG1", "value1" },
                { "arg2", "value2" },
                { "Arg3", "value3" }
            }
        };

        // Act
        execInfo.RemoveAllArgumentsExcept(false, "arg1", "ARG3");

        // Assert
        Assert.AreEqual(2, execInfo.Arguments.Count);
        Assert.IsTrue(execInfo.Arguments.ContainsKey("ARG1"));
        Assert.IsTrue(execInfo.Arguments.ContainsKey("Arg3"));
        Assert.IsFalse(execInfo.Arguments.ContainsKey("arg2"));
    }

    [TestMethod]
    public void RemoveAllArgumentsExcept_RemovesCommonArguments_WhenPreserveCommonArgumentsIsFalse()
    {
        // Arrange
        var execInfo = new CommandExecutionInfo
        {
            Arguments = new Dictionary<string, string>
            {
                { Constants.ArgumentNameQuietMode, "true" },
                { Constants.ArgumentNameConfigurationName, "config" },
                { "arg1", "value1" }
            }
        };

        // Act
        execInfo.RemoveAllArgumentsExcept(false, "arg1");

        // Assert
        Assert.AreEqual(1, execInfo.Arguments.Count);
        Assert.IsTrue(execInfo.Arguments.ContainsKey("arg1"));
        Assert.IsFalse(execInfo.Arguments.ContainsKey(Constants.ArgumentNameQuietMode));
        Assert.IsFalse(execInfo.Arguments.ContainsKey(Constants.ArgumentNameConfigurationName));
    }

    [TestMethod]
    public void RemoveAllArgumentsExcept_HandlesEmptyArgumentsToKeep_WhenPreservingCommonArguments()
    {
        // Arrange
        var execInfo = new CommandExecutionInfo
        {
            Arguments = new Dictionary<string, string>
            {
                { Constants.ArgumentNameQuietMode, "true" },
                { "arg1", "value1" }
            }
        };

        // Act
        execInfo.RemoveAllArgumentsExcept(true, Array.Empty<string>());

        // Assert
        Assert.AreEqual(1, execInfo.Arguments.Count);
        Assert.IsTrue(execInfo.Arguments.ContainsKey(Constants.ArgumentNameQuietMode));
    }
}