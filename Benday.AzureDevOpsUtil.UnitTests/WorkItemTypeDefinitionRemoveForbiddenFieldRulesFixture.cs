using Benday.AzureDevOpsUtil.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        public void RemoveForAndNotAttributes_OneWitd()
        {
            // arrange
            var filesToCheck = GetFilesToCheck();

            var fileToCheck = filesToCheck.Where(x => x.Contains("IES_LE_PMI")).FirstOrDefault();

            var witd = new WorkItemTypeDefinition(fileToCheck);

            Assert.IsTrue(witd.HasForAndNotAttributes(), "Should have forbidden attrs before");

            var beforeXml = witd.Element.ToString();
            Assert.IsTrue(beforeXml.Contains(" for=\""), "Should contain 'for' attributes");
            Assert.IsTrue(beforeXml.Contains(" not=\""), "Should contain 'not' attributes");

            // act
            witd.RemoveForAndNotAttributes();

            // assert
            Assert.IsFalse(witd.HasForAndNotAttributes(), "Should not have forbidden attrs after");
            var afterXml = witd.Element.ToString();
            Assert.IsFalse(afterXml.Contains(" for=\""), "Should not contain 'for' attributes");
            Assert.IsFalse(afterXml.Contains(" not=\""), "Should not contain 'not' attributes");
        }

        [TestMethod]
        public void BugWorkItemUpdaterForMissingFields_EnsureRequiredFieldsOnBug_OneWitd()
        {
            // arrange
            var filesToCheck = GetFilesToCheck();

            var fileToCheck = filesToCheck.Where(x => x.Contains("GMS_BT_BASE") && x.Contains("bug-clean")).FirstOrDefault();

            var witd = new WorkItemTypeDefinition(fileToCheck);

            var refname1 = "Microsoft.VSTS.TCM.ReproSteps";
            var refname2 = "Microsoft.VSTS.TCM.SystemInfo";
            var refname3 = "Microsoft.VSTS.Common.AcceptanceCriteria";
            var pageName = "Misc";

            /*
                  <FIELD name="System Info" refname="Microsoft.VSTS.TCM.SystemInfo" type="HTML" />
      <FIELD name="Repro Steps" refname="Microsoft.VSTS.TCM.ReproSteps" type="HTML" />
            */

            Assert.IsFalse(witd.HasField(refname1), $"Should not have field for {refname1}");
            Assert.IsFalse(witd.HasFieldInWebLayout(refname1), $"Should not display field for {refname1}");

            Assert.IsFalse(witd.HasField(refname2), $"Should not have field for {refname2}");
            Assert.IsFalse(witd.HasFieldInWebLayout(refname2), $"Should not display field for {refname2}");

            Assert.IsFalse(witd.HasField(refname3), $"Should not have field for {refname3}");
            Assert.IsFalse(witd.HasFieldInWebLayout(refname3), $"Should not display field for {refname3}");

            Assert.IsTrue(witd.HasPageInWebLayout("Description"), $"Should have web layout page named Description");
            Assert.IsFalse(witd.HasPageInWebLayout(pageName), $"Should not have web layout page named {pageName}");

            // act
            var bugFixer = new BugWorkItemUpdaterForMissingFields(witd);
            bugFixer.Fix();

            // assert
            Assert.IsTrue(witd.HasField(refname1), $"Should have field for {refname1}");
            Assert.IsTrue(witd.HasFieldInWebLayout(refname1), $"Should display field for {refname1}");

            Assert.IsTrue(witd.HasField(refname2), $"Should have field for {refname2}");
            Assert.IsTrue(witd.HasFieldInWebLayout(refname2), $"Should display field for {refname2}");

            Assert.IsTrue(witd.HasField(refname3), $"Should have field for {refname3}");
            Assert.IsTrue(witd.HasFieldInWebLayout(refname3), $"Should display field for {refname3}");

            Assert.IsTrue(witd.HasPageInWebLayout(pageName), $"Should have web layout page named {pageName}");
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
