using Benday.AzureDevOpsUtil.Api.SecurityMigration;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class LocalGroupExportServiceFixture
{
    private const string MachineName = "APPTIER01";

    private const string LocalGroupDescriptor =
        "System.Security.PrincipalIdentity;S-1-5-21-1111111111-2222222222-3333333333-1010";

    private const string DomainGroupDescriptor =
        "System.Security.PrincipalIdentity;S-1-5-21-4444444444-5555555555-6666666666-2020";

    private const string InternalGroupDescriptor =
        "Microsoft.TeamFoundation.Identity;S-1-9-1551374245-12345-67890-111-1";

    private const string DomainUserDescriptor =
        "System.Security.PrincipalIdentity;S-1-5-21-4444444444-5555555555-6666666666-3030";

    private const string LocalUserDescriptor =
        "System.Security.PrincipalIdentity;S-1-5-21-1111111111-2222222222-3333333333-4040";

    private static FakeSecurityApiClient CreateFakeClient()
    {
        var fake = new FakeSecurityApiClient();

        var buildNamespace = new SecurityNamespaceInfo
        {
            NamespaceId = "33344d9c-fc72-4d6f-aba5-fa317101a739",
            Name = "Build",
            DisplayName = "Build"
        };

        buildNamespace.Actions.Add(new SecurityNamespaceAction
        {
            Bit = 1, Name = "ViewBuilds", DisplayName = "View builds"
        });
        buildNamespace.Actions.Add(new SecurityNamespaceAction
        {
            Bit = 2, Name = "EditBuildQuality", DisplayName = "Edit build quality"
        });
        buildNamespace.Actions.Add(new SecurityNamespaceAction
        {
            Bit = 8, Name = "DeleteBuilds", DisplayName = "Delete builds"
        });

        fake.Namespaces.Add(buildNamespace);

        fake.AclsByNamespaceId[buildNamespace.NamespaceId] = new List<AccessControlListInfo>
        {
            new()
            {
                Token = "$PROJECT:vstfs:///Classification/TeamProject/aaaa",
                InheritPermissions = true,
                Entries =
                {
                    new() { Descriptor = LocalGroupDescriptor, Allow = 3, Deny = 8 },
                    new() { Descriptor = InternalGroupDescriptor, Allow = 1, Deny = 0 },
                    new() { Descriptor = DomainGroupDescriptor, Allow = 1, Deny = 0 }
                }
            }
        };

        fake.IdentitiesByDescriptor[LocalGroupDescriptor] = new TfsIdentityInfo
        {
            Descriptor = LocalGroupDescriptor,
            ProviderDisplayName = @"APPTIER01\Build Admins",
            IsContainer = true,
            Account = "Build Admins",
            Domain = "APPTIER01",
            SchemaClassName = "Group",
            MemberDescriptors = { DomainUserDescriptor, LocalUserDescriptor }
        };

        fake.IdentitiesByDescriptor[DomainGroupDescriptor] = new TfsIdentityInfo
        {
            Descriptor = DomainGroupDescriptor,
            ProviderDisplayName = @"CONTOSO\Developers",
            IsContainer = true,
            Account = "Developers",
            Domain = "CONTOSO",
            SchemaClassName = "Group"
        };

        fake.IdentitiesByDescriptor[InternalGroupDescriptor] = new TfsIdentityInfo
        {
            Descriptor = InternalGroupDescriptor,
            ProviderDisplayName = "[MyProject]\\Contributors",
            IsContainer = true,
            Account = "Contributors",
            Domain = "vstfs:///Framework/IdentityDomain/some-guid",
            SchemaClassName = "Group"
        };

        fake.IdentitiesByDescriptor[DomainUserDescriptor] = new TfsIdentityInfo
        {
            Descriptor = DomainUserDescriptor,
            ProviderDisplayName = @"CONTOSO\jsmith",
            Account = "jsmith",
            Domain = "CONTOSO",
            SchemaClassName = "User"
        };

        fake.IdentitiesByDescriptor[LocalUserDescriptor] = new TfsIdentityInfo
        {
            Descriptor = LocalUserDescriptor,
            ProviderDisplayName = @"APPTIER01\buildsvc",
            Account = "buildsvc",
            Domain = "APPTIER01",
            SchemaClassName = "User"
        };

        return fake;
    }

    [TestMethod]
    public async Task Export_FindsLocalGroupsOnly()
    {
        // arrange
        var fake = CreateFakeClient();
        var systemUnderTest = new LocalGroupExportService(fake);

        // act
        var actual = await systemUnderTest.ExportAsync(MachineName, "https://tfs/DefaultCollection/");

        // assert
        Assert.AreEqual<int>(1, actual.Document.Groups.Count,
            "only the machine-local group is exported -- not the AD group, not the TFS group");

        var group = actual.Document.Groups[0];
        Assert.AreEqual<string>(@"APPTIER01\Build Admins", group.AccountName, "account name");
        Assert.AreEqual<string>("Build Admins", group.GroupName, "group name without machine prefix");
        Assert.AreEqual<int>(2, group.Members.Count, "member count");

        var domainMember = group.Members.Single(x => x.AccountName == @"CONTOSO\jsmith");
        Assert.AreEqual<bool>(false, domainMember.IsMachineLocal, "domain member is not machine local");

        var localMember = group.Members.Single(x => x.AccountName == @"APPTIER01\buildsvc");
        Assert.AreEqual<bool>(true, localMember.IsMachineLocal, "machine local member is flagged");
    }

    [TestMethod]
    public async Task Export_KeepsOnlyTheLocalGroupsAces_AndDecodesActionNames()
    {
        // arrange
        var fake = CreateFakeClient();
        var systemUnderTest = new LocalGroupExportService(fake);

        // act
        var actual = await systemUnderTest.ExportAsync(MachineName, "https://tfs/DefaultCollection/");

        // assert
        Assert.AreEqual<int>(1, actual.Document.AccessControlEntries.Count, "ace count");

        var ace = actual.Document.AccessControlEntries[0];
        Assert.AreEqual<string>(@"APPTIER01\Build Admins", ace.GroupAccountName, "group account name");
        Assert.AreEqual<string>("Build", ace.NamespaceName, "namespace name");
        Assert.AreEqual<int>(3, ace.Allow, "allow bits");
        Assert.AreEqual<int>(8, ace.Deny, "deny bits");
        CollectionAssert.AreEqual(new[] { "ViewBuilds", "EditBuildQuality" }, ace.AllowActionNames,
            "allow action names decoded from bits");
        CollectionAssert.AreEqual(new[] { "DeleteBuilds" }, ace.DenyActionNames,
            "deny action names decoded from bits");
    }

    [TestMethod]
    public async Task Export_ReportsWindowsDomainsSeen()
    {
        // arrange
        var fake = CreateFakeClient();
        var systemUnderTest = new LocalGroupExportService(fake);

        // act -- note the machine name matches nothing
        var actual = await systemUnderTest.ExportAsync("WRONGMACHINE", "https://tfs/DefaultCollection/");

        // assert
        Assert.AreEqual<int>(0, actual.Document.Groups.Count, "no groups for the wrong machine");
        CollectionAssert.AreEqual(new[] { "APPTIER01", "CONTOSO" }, actual.WindowsDomainsSeen,
            "windows domains seen -- internal TFS groups are excluded");
    }
}
