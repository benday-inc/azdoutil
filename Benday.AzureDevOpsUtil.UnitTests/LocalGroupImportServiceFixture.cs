using Benday.AzureDevOpsUtil.Api.SecurityMigration;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class LocalGroupImportServiceFixture
{
    private const string NewMachineName = "NEWAPPTIER";

    private const string NewGroupDescriptor =
        "System.Security.PrincipalIdentity;S-1-5-21-7777777777-8888888888-9999999999-1010";

    private static LocalGroupsExportDocument CreateDocument()
    {
        return new LocalGroupsExportDocument
        {
            CollectionUrl = "https://tfs/DefaultCollection/",
            MachineName = "APPTIER01",
            Groups =
            {
                new ExportedLocalGroup
                {
                    AccountName = @"APPTIER01\Build Admins",
                    GroupName = "Build Admins"
                },
                new ExportedLocalGroup
                {
                    AccountName = @"APPTIER01\Missing Group",
                    GroupName = "Missing Group"
                }
            },
            AccessControlEntries =
            {
                new ExportedAccessControlEntry
                {
                    NamespaceId = "ns-1",
                    NamespaceName = "Build",
                    Token = "token-a",
                    GroupAccountName = @"APPTIER01\Build Admins",
                    Allow = 3,
                    Deny = 8
                },
                new ExportedAccessControlEntry
                {
                    NamespaceId = "ns-1",
                    NamespaceName = "Build",
                    Token = "token-b",
                    GroupAccountName = @"APPTIER01\Build Admins",
                    Allow = 1,
                    Deny = 0
                },
                new ExportedAccessControlEntry
                {
                    NamespaceId = "ns-1",
                    NamespaceName = "Build",
                    Token = "token-a",
                    GroupAccountName = @"APPTIER01\Missing Group",
                    Allow = 4,
                    Deny = 0
                }
            }
        };
    }

    private static FakeSecurityApiClient CreateFakeClient()
    {
        var fake = new FakeSecurityApiClient();

        fake.IdentitiesByAccountName[$@"{NewMachineName}\Build Admins"] = new TfsIdentityInfo
        {
            Descriptor = NewGroupDescriptor,
            ProviderDisplayName = $@"{NewMachineName}\Build Admins",
            IsContainer = true,
            Account = "Build Admins",
            Domain = NewMachineName,
            SchemaClassName = "Group"
        };

        return fake;
    }

    [TestMethod]
    public async Task Import_AppliesResolvedGroupsAcesWithNewDescriptor()
    {
        // arrange
        var fake = CreateFakeClient();
        var systemUnderTest = new LocalGroupImportService(fake);

        // act
        var actual = await systemUnderTest.ImportAsync(CreateDocument(), NewMachineName, preview: false);

        // assert
        Assert.AreEqual<int>(1, actual.ResolvedGroups.Count, "resolved group count");
        CollectionAssert.AreEqual(new[] { @"APPTIER01\Missing Group" }, actual.UnresolvedGroups,
            "unresolved group count");

        Assert.AreEqual<int>(2, actual.TokenCount, "token count -- one call per token");
        Assert.AreEqual<int>(2, actual.AppliedAceCount, "applied ace count");
        Assert.AreEqual<int>(2, fake.SetAccessControlEntriesCalls.Count, "server calls");

        var tokenACall = fake.SetAccessControlEntriesCalls
            .Single(x => x.Token == "token-a");

        Assert.AreEqual<int>(1, tokenACall.Entries.Count,
            "the missing group's ace on token-a is not applied");
        Assert.AreEqual<string>(NewGroupDescriptor, tokenACall.Entries[0].Descriptor,
            "ace carries the NEW server's descriptor");
        Assert.AreEqual<int>(3, tokenACall.Entries[0].Allow, "allow bits");
        Assert.AreEqual<int>(8, tokenACall.Entries[0].Deny, "deny bits");
    }

    [TestMethod]
    public async Task Import_Preview_ResolvesButDoesNotCallServer()
    {
        // arrange
        var fake = CreateFakeClient();
        var systemUnderTest = new LocalGroupImportService(fake);

        // act
        var actual = await systemUnderTest.ImportAsync(CreateDocument(), NewMachineName, preview: true);

        // assert
        Assert.AreEqual<bool>(true, actual.Preview, "preview flag");
        Assert.AreEqual<int>(1, actual.ResolvedGroups.Count, "resolved group count");
        Assert.AreEqual<int>(2, actual.AppliedAceCount, "would-apply ace count");
        Assert.AreEqual<int>(0, fake.SetAccessControlEntriesCalls.Count,
            "no server calls in preview mode");
    }

    [TestMethod]
    public async Task Import_ServerRefusal_RecordsFailedToken()
    {
        // arrange
        var fake = CreateFakeClient();
        fake.SetAccessControlEntriesResult = false;
        var systemUnderTest = new LocalGroupImportService(fake);

        // act
        var actual = await systemUnderTest.ImportAsync(CreateDocument(), NewMachineName, preview: false);

        // assert
        Assert.AreEqual<int>(0, actual.AppliedAceCount, "nothing applied");
        Assert.AreEqual<int>(2, actual.FailedTokens.Count, "failed token count");
    }
}
