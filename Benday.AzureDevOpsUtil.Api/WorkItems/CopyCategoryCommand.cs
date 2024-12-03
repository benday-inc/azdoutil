using System.Text.Json;
using System.Text;
using System.Web;
using System.Xml.Linq;
using Benday.AzureDevOpsUtil.Api.Excel;
using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;
using Benday.XmlUtilities;

namespace Benday.AzureDevOpsUtil.Api.WorkItems;

[Command(
    Category = Constants.Category_WorkItems,
    Name = Constants.CommandArgumentNameCopyCategory,
    Description = "Copy category type from one category file to another.", IsAsync = true)]
public class CopyCategoryCommand : AzureDevOpsCommandBase
{

    public CopyCategoryCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddFile(Constants.ArgumentNameComparisonFile1).AsRequired()
            .MustExist()
            .WithDescription("Path to the source category definition file.")
            .FromPositionalArgument(1);

        args.AddFile(Constants.ArgumentNameComparisonFile2).AsRequired()
            .MustExist()            
            .WithDescription("Path to the target category definition file.")
            .FromPositionalArgument(2);

        args.AddString(Constants.ArgumentNameFieldRefName).AsRequired().
            WithDescription("Refname of the category to copy.");

        args.AddBoolean(Constants.ArgumentNameOverwrite).AsRequired().
            WithDescription("Overwrite the target field if it already exists.")
            .WithDefaultValue(false);

        return args;
    }


    protected override Task OnExecute()
    {
        var file1 = Arguments.GetPathToFile(Constants.ArgumentNameComparisonFile1, true);
        var file2 = Arguments.GetPathToFile(Constants.ArgumentNameComparisonFile2, true);

        var refname = Arguments.GetStringValue(Constants.ArgumentNameFieldRefName);

        var overwrite = Arguments.GetBooleanValue(Constants.ArgumentNameOverwrite);

        if (overwrite == false)
        {
            WriteLine("Overwrite is disabled.  Existing fields will not be replaced.");
        }
        else
        {
            WriteLine("Overwrite is enabled.  Existing fields will be replaced.");
        }

        WriteLine("Loading files...");

        WriteLine($"Source file: {file1}");
        var source = LoadAndValidateCategory(file1);

        WriteLine($"Target file: {file2}");
        var target = LoadAndValidateCategory(file2);

        CopyCategory(source, target, refname, overwrite);

        WriteLine("Field copied.");

        target.Save(file2);

        WriteLine("Saved target file.");

        return Task.CompletedTask;
    }

    private XElement LoadAndValidateCategory(string pathToFile)
    {
        var result = XElement.Load(pathToFile);

        if (result == null)
        {
            throw new KnownException($"Unable to load file '{pathToFile}'.");
        }

        if (result.Name.LocalName != "CATEGORIES")
        {
            throw new KnownException($"File '{pathToFile}' is not a valid category file. Root element must be 'CATEGORIES' but was '{result.Name.LocalName}'.");
        }

        return result;
    }


    private void CopyCategory(XElement source, XElement target, string refname, bool overwrite)
    {
        var sourceCategory = 
            source.Elements("CATEGORY").FirstOrDefault(x => String.Equals(x.AttributeValue("refname"), refname, StringComparison.OrdinalIgnoreCase));

        var targetCategory = 
            target.Elements("CATEGORY").FirstOrDefault(x => String.Equals(x.AttributeValue("refname"), refname, StringComparison.OrdinalIgnoreCase));

        if (sourceCategory == null)
        {
            throw new KnownException($"Category with refname '{refname}' not found in source file.");
        }

        if (targetCategory != null && overwrite == false)
        {
            throw new KnownException($"Category with refname '{refname}' already exists in target file.");
        }
        else if (targetCategory != null && overwrite == true)
        {
            WriteLine($"Overwriting category '{refname}' in target file.");
        }

        if (targetCategory == null)
        {
            WriteLine($"Adding category '{refname}' to target file.");
            target.Add(sourceCategory);
        }
        else
        {
            WriteLine($"Replacing category '{refname}' in target file.");
            targetCategory.ReplaceWith(sourceCategory);
        }
    }

    
}
