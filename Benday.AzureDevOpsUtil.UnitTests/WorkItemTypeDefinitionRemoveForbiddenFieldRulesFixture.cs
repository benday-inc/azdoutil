using Benday.AzureDevOpsUtil.Api;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Text;

namespace Benday.AzureDevOpsUtil.UnitTests
{
    [TestClass]
    [Ignore("Need to migrate test files into solution")]
    public class WorkItemTypeDefinitionRemoveForbiddenFieldRulesFixture
    {
        private WorkItemTypeDefinition? _systemUnderTest;
        public WorkItemTypeDefinition SystemUnderTest
        {
            get
            {
                if (_systemUnderTest == null)
                {
                    _systemUnderTest = new WorkItemTypeDefinition(
                        UnitTestUtility.GetPathToTestFile());
                }

                return _systemUnderTest;
            }
        }

        [TestMethod]
        public void FindFilesToCheck()
        {
            var filesToCheck = GetFilesToCheck();

            filesToCheck.ForEach(x => Console.WriteLine(x));
        }

        [TestMethod]
        public void RemoveForAndNotAttributes_GenerateWitdsAndUploadScript()
        {
            // arrange
            var filesToCheck = GetFilesToCheck();
            var builder = new StringBuilder();

            foreach (var fileToCheck in filesToCheck)
            {
                var witd = new WorkItemTypeDefinition(fileToCheck);
                if (witd.HasForAndNotAttributes() == true)
                {
                    var dir = new FileInfo(fileToCheck).Directory!;
                    var teamProjectName = dir.Name;

                    if (witd.WorkItemType.ToLower() == "bug-clean")
                    {
                        witd.WorkItemType = "Bug";
                        var toFile = Path.Combine(dir.FullName, "bug-without-forbidden-attributes.xml");
                        witd.RemoveForAndNotAttributes();

                        var bugFixer = new BugWorkItemUpdaterForMissingFields(witd);
                        bugFixer.Fix();

                        witd.Save(toFile);
                        AddWitImportForFile(builder, teamProjectName, toFile);
                    }
                    else if (witd.WorkItemType.ToLower() == "change-request-clean")
                    {
                        witd.WorkItemType = "Change Request";
                        var toFile = Path.Combine(dir.FullName, "change-request-without-forbidden-attributes.xml");
                        witd.RemoveForAndNotAttributes();
                        witd.Save(toFile);
                        AddWitImportForFile(builder, teamProjectName, toFile);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unsupported work item type: {witd.WorkItemType} @ {fileToCheck}");
                    }
                }
            }

            var scriptsDir = @"C:\Users\benday\code\AzureDevOpsWorkItemUtility\migrator-temp\";
            var scriptFilePath = Path.Combine(scriptsDir, "05-upload-witds-without-forbidden-attrs.bat");
            File.WriteAllText(scriptFilePath, builder.ToString());
        }

        private void AddWitImportForFile(StringBuilder builder, string teamProjectName, string toFile)
        {
            builder.Append("witadmin importwitd /collection:https://usmdckqap5775.us.kworld.kpmg.com/TtpDefaultCollection/ /p:\"");
            builder.Append(teamProjectName);
            builder.Append("\" /f:\"");
            builder.Append(toFile);
            builder.AppendLine("\"");
        }

        private static List<string> GetFilesToCheck()
        {
            var pathToCheck = @"C:\Users\benday\code\AzureDevOpsWorkItemUtility\migrator-temp";

            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true
            };
            var filesToCheck = Directory.EnumerateFiles(pathToCheck, "bug-clean.xml", options).ToList();
            filesToCheck.AddRange(Directory.EnumerateFiles(pathToCheck, "change-request-clean.xml", options));
            return filesToCheck;
        }
    }
}
