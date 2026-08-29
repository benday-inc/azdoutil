using Benday.AzureDevOpsUtil.Api.SecurityMigration;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class LocalGroupPowerShellScriptGeneratorFixture
{
    private static LocalGroupsExportDocument CreateDocument()
    {
        return new LocalGroupsExportDocument
        {
            CollectionUrl = "https://tfs/DefaultCollection/",
            MachineName = "APPTIER01",
            ExportedAtUtc = new DateTime(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc),
            Groups =
            {
                new ExportedLocalGroup
                {
                    AccountName = @"APPTIER01\Build Admins",
                    GroupName = "Build Admins",
                    Members =
                    {
                        new ExportedGroupMember
                        {
                            AccountName = @"CONTOSO\jsmith",
                            IsMachineLocal = false
                        },
                        new ExportedGroupMember
                        {
                            AccountName = @"APPTIER01\buildsvc",
                            IsMachineLocal = true
                        }
                    }
                },
                new ExportedLocalGroup
                {
                    AccountName = @"APPTIER01\O'Brien's Team",
                    GroupName = "O'Brien's Team"
                }
            }
        };
    }

    [TestMethod]
    public void Generate_CreatesGroupsAndAddsDomainMembers()
    {
        // arrange
        var document = CreateDocument();

        // act
        var actual = LocalGroupPowerShellScriptGenerator.Generate(document);

        // assert
        StringAssert.Contains(actual, "New-LocalGroup -Name 'Build Admins'", "creates the group");
        StringAssert.Contains(actual, "Get-LocalGroup -Name 'Build Admins' -ErrorAction SilentlyContinue",
            "existence check makes the script rerunnable");
        StringAssert.Contains(actual,
            @"Add-LocalGroupMember -Group 'Build Admins' -Member 'CONTOSO\jsmith'",
            "adds the domain member");
    }

    [TestMethod]
    public void Generate_MachineLocalMembers_AreCalledOutInsteadOfAdded()
    {
        // arrange
        var document = CreateDocument();

        // act
        var actual = LocalGroupPowerShellScriptGenerator.Generate(document);

        // assert
        Assert.AreEqual<bool>(false,
            actual.Contains(@"Add-LocalGroupMember -Group 'Build Admins' -Member 'APPTIER01\buildsvc'"),
            "old machine's local account is not added by name");
        StringAssert.Contains(actual, @"# NOT ADDED: 'APPTIER01\buildsvc'",
            "the member is called out for a human to handle");
    }

    [TestMethod]
    public void Generate_EscapesSingleQuotesInNames()
    {
        // arrange
        var document = CreateDocument();

        // act
        var actual = LocalGroupPowerShellScriptGenerator.Generate(document);

        // assert
        StringAssert.Contains(actual, "New-LocalGroup -Name 'O''Brien''s Team'",
            "single quotes are doubled for PowerShell");
    }

    [TestMethod]
    public void Generate_NullDocument_Throws()
    {
        // act & assert
        Assert.ThrowsExactly<ArgumentNullException>(
            () => LocalGroupPowerShellScriptGenerator.Generate(null!));
    }
}
