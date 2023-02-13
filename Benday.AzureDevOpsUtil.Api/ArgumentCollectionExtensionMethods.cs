using Benday.CommandsFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.AzureDevOpsUtil.Api;
public static class ExtensionMethods
{
    public static CommandExecutionInfo GetCloneOfArguments(
        this CommandExecutionInfo execInfo, string commandName, bool quietMode)
    {
        if (execInfo is null || execInfo.Arguments is null)
        {
            throw new ArgumentNullException(nameof(execInfo));
        }

        var argsClone = execInfo.Arguments.ToDictionary(entry => entry.Key, entry => entry.Value);
                
        if (quietMode == true)
        {
            argsClone.Add(Constants.ArgumentNameQuietMode, "true");
        }

        var returnValue = new CommandExecutionInfo();
        returnValue.Arguments= argsClone;
        returnValue.CommandName = commandName;

        return returnValue;
    }
}
