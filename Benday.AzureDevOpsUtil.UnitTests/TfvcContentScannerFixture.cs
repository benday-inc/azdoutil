using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.AzureDevOpsUtil.Api.TfvcAssessment;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class TfvcContentScannerFixture
{
    private TfvcContentScanner SystemUnderTest => new();

    private const string Scope = "$/GnarlyCorp/Main";

    private const long OneMegabyte = 1024L * 1024L;

    private static TfvcItemInfo File(string path, long sizeBytes)
    {
        return new TfvcItemInfo
        {
            Path = path,
            Size = sizeBytes
        };
    }

    private static TfvcItemInfo Folder(string path)
    {
        return new TfvcItemInfo
        {
            Path = path,
            IsFolder = true
        };
    }

    [TestMethod]
    public void Scan_CountsFilesAndSize()
    {
        var items = new[]
        {
            Folder(Scope),
            Folder($"{Scope}/src"),
            File($"{Scope}/src/Program.cs", 2048),
            File($"{Scope}/src/App.config", 1024)
        };

        var actual = SystemUnderTest.Scan(items, Scope);

        Assert.AreEqual(2, actual.FileCount, "Folders are not files.");
        Assert.AreEqual(3072, actual.TotalSizeBytes, "Wrong total size.");
    }

    [TestMethod]
    public void Scan_IgnoresFoldersEvenWhenTheyCarryNoSize()
    {
        var items = new[] { Folder(Scope), Folder($"{Scope}/bin") };

        var actual = SystemUnderTest.Scan(items, Scope);

        Assert.AreEqual(0, actual.FileCount, "A tree of folders holds no files.");
        Assert.AreEqual(
            0, actual.GeneratedFolders.Count, "An empty bin folder contains nothing to report.");
    }

    [TestMethod]
    public void Scan_RanksTheLargestFiles()
    {
        var items = new[]
        {
            File($"{Scope}/small.txt", 100),
            File($"{Scope}/huge.iso", 900 * OneMegabyte),
            File($"{Scope}/medium.zip", 5 * OneMegabyte)
        };

        var actual = SystemUnderTest.Scan(items, Scope);

        Assert.AreEqual(3, actual.LargestFiles.Count, "Expected every file to be ranked.");
        Assert.AreEqual($"{Scope}/huge.iso", actual.LargestFiles[0].Path, "Wrong largest file.");
        Assert.AreEqual($"{Scope}/small.txt", actual.LargestFiles[2].Path, "Wrong smallest file.");
    }

    [TestMethod]
    public void Scan_LimitsHowManyLargestFilesAreKept()
    {
        var items = Enumerable.Range(1, 50)
            .Select(x => File($"{Scope}/file{x}.bin", x * 1024L))
            .ToArray();

        var actual = SystemUnderTest.Scan(items, Scope, largestFileCount: 5);

        Assert.AreEqual(5, actual.LargestFiles.Count, "Wrong number of largest files kept.");
        Assert.AreEqual(50, actual.FileCount, "Every file should still be counted.");
    }

    [TestMethod]
    public void Scan_CountsFilesAgainstTheHostingThresholds()
    {
        var items = new[]
        {
            File($"{Scope}/tiny.txt", 1024),
            File($"{Scope}/big.bak", 60 * OneMegabyte),
            File($"{Scope}/huge.iso", 300 * OneMegabyte)
        };

        var actual = SystemUnderTest.Scan(items, Scope);

        Assert.AreEqual(2, actual.FilesOverWarningSize, "Both large files pass 50 MB.");
        Assert.AreEqual(1, actual.FilesOverPushLimit, "Only one file passes 100 MB.");
    }

    [TestMethod]
    public void Scan_GroupsExtensionsOfInterest()
    {
        var items = new[]
        {
            File($"{Scope}/a.dll", 1000),
            File($"{Scope}/b.dll", 2000),
            File($"{Scope}/c.exe", 5000),
            File($"{Scope}/Program.cs", 900)
        };

        var actual = SystemUnderTest.Scan(items, Scope);

        var dll = actual.ExtensionUsages.Single(x => x.Extension == ".dll");

        Assert.AreEqual(2, dll.FileCount, "Wrong dll count.");
        Assert.AreEqual(3000, dll.TotalSizeBytes, "Wrong dll size.");

        Assert.IsFalse(
            actual.ExtensionUsages.Any(x => x.Extension == ".cs"),
            "Source files are not build output.");

        // Biggest first, because that is the row worth reading.
        Assert.AreEqual(".exe", actual.ExtensionUsages[0].Extension, "Wrong sort order.");
    }

    [TestMethod]
    public void Scan_ExtensionMatchingIgnoresCase()
    {
        var items = new[] { File($"{Scope}/A.DLL", 1000) };

        var actual = SystemUnderTest.Scan(items, Scope);

        Assert.AreEqual(
            ".dll", actual.ExtensionUsages.Single().Extension, "Extensions should normalize.");
    }

    [TestMethod]
    public void Scan_FindsGeneratedFolders()
    {
        var items = new[]
        {
            File($"{Scope}/src/Program.cs", 500),
            File($"{Scope}/src/bin/App.dll", 1000),
            File($"{Scope}/src/obj/App.pdb", 2000),
            File($"{Scope}/packages/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll", 4000)
        };

        var actual = SystemUnderTest.Scan(items, Scope);

        Assert.AreEqual(3, actual.GeneratedFolders.Count, "Expected bin, obj and packages.");

        var packages = actual.GeneratedFolders.Single(x => x.Name == "packages");

        Assert.AreEqual(1, packages.FileCount, "Wrong packages file count.");
        Assert.AreEqual(4000, packages.TotalSizeBytes, "Wrong packages size.");

        Assert.AreEqual(3, actual.GeneratedFolderFileCount, "Wrong overall count.");
        Assert.AreEqual(7000, actual.GeneratedFolderSizeBytes, "Wrong overall size.");
    }

    [TestMethod]
    public void Scan_CountsANestedFileOnceAgainstTheOutermostFolder()
    {
        // A dll inside packages/.../bin should not be counted twice.
        var items = new[]
        {
            File($"{Scope}/packages/Some.Package/tools/bin/thing.dll", 1000)
        };

        var actual = SystemUnderTest.Scan(items, Scope);

        Assert.AreEqual(1, actual.GeneratedFolders.Count, "Expected a single attribution.");
        Assert.AreEqual(
            "packages", actual.GeneratedFolders[0].Name, "Should attribute to the outermost folder.");
        Assert.AreEqual(1, actual.GeneratedFolderFileCount, "The file should be counted once.");
    }

    [TestMethod]
    public void Scan_GeneratedFolderMatchingIgnoresCase()
    {
        var items = new[] { File($"{Scope}/src/BIN/App.dll", 1000) };

        var actual = SystemUnderTest.Scan(items, Scope);

        Assert.AreEqual(
            "bin",
            actual.GeneratedFolders.Single().Name,
            "The canonical spelling should be reported.");
    }

    [TestMethod]
    public void Scan_MatchesReSharperFoldersOnTheirPrefix()
    {
        var items = new[]
        {
            File($"{Scope}/_ReSharper.MySolution/cache.dat", 1000)
        };

        var actual = SystemUnderTest.Scan(items, Scope);

        Assert.AreEqual(
            "_ReSharper*", actual.GeneratedFolders.Single().Name, "Wrong folder name.");
    }

    [TestMethod]
    public void Scan_DoesNotFlagAmbiguousFolderNames()
    {
        var items = new[]
        {
            File($"{Scope}/build/build.proj", 500),
            File($"{Scope}/lib/Helper.cs", 500),
            File($"{Scope}/out/Readme.md", 500),
            File($"{Scope}/Release/Notes.txt", 500),
            File($"{Scope}/Debug/Notes.txt", 500)
        };

        var actual = SystemUnderTest.Scan(items, Scope);

        Assert.AreEqual(
            0,
            actual.GeneratedFolders.Count,
            "Names that are legitimately source folders should not be flagged.");
    }

    [TestMethod]
    public void Scan_FolderNameMustBeAWholeSegment()
    {
        var items = new[]
        {
            File($"{Scope}/binaries/thing.txt", 500),
            File($"{Scope}/src/objects/Thing.cs", 500)
        };

        var actual = SystemUnderTest.Scan(items, Scope);

        Assert.AreEqual(
            0, actual.GeneratedFolders.Count, "A name prefix is not a folder name.");
    }

    [TestMethod]
    public void Scan_FileDirectlyUnderTheScopeHasNoFolders()
    {
        var items = new[] { File($"{Scope}/readme.txt", 100) };

        var actual = SystemUnderTest.Scan(items, Scope);

        Assert.AreEqual(0, actual.GeneratedFolders.Count, "There are no folders in between.");
    }

    [TestMethod]
    public void Scan_ScopeSegmentsAreNotExamined()
    {
        // The scope path itself is not searched for folder names, only what
        // sits below it.
        var items = new[] { File("$/App/bin/Main/src/Program.cs", 100) };

        var actual = SystemUnderTest.Scan(items, "$/App/bin/Main");

        Assert.AreEqual(
            0, actual.GeneratedFolders.Count, "The scope path is not part of the search.");
    }

    [TestMethod]
    public void Scan_EmptyAndNullInput()
    {
        Assert.AreEqual(
            0, SystemUnderTest.Scan(null, Scope).FileCount, "Null should scan to nothing.");

        Assert.AreEqual(
            0,
            SystemUnderTest.Scan(Array.Empty<TfvcItemInfo>(), Scope).FileCount,
            "Empty should scan to nothing.");
    }

    [TestMethod]
    public void Scan_MissingSizeCountsAsZero()
    {
        var items = new[] { new TfvcItemInfo { Path = $"{Scope}/mystery.bin" } };

        var actual = SystemUnderTest.Scan(items, Scope);

        Assert.AreEqual(1, actual.FileCount, "The file should still be counted.");
        Assert.AreEqual(0, actual.TotalSizeBytes, "A missing size contributes nothing.");
    }

    [TestMethod]
    public void GetExtension_HandlesDotfilesAndTrailingDots()
    {
        Assert.AreEqual(".dll", TfvcContentScanner.GetExtension("$/App/thing.dll"), "Wrong ext.");
        Assert.AreEqual(
            string.Empty,
            TfvcContentScanner.GetExtension("$/App/.tfignore"),
            "A dotfile has no extension.");
        Assert.AreEqual(
            string.Empty,
            TfvcContentScanner.GetExtension("$/App/thing."),
            "A trailing dot is not an extension.");
        Assert.AreEqual(
            string.Empty,
            TfvcContentScanner.GetExtension("$/App/Makefile"),
            "No dot means no extension.");
        Assert.AreEqual(
            ".gz",
            TfvcContentScanner.GetExtension("$/App/archive.tar.gz"),
            "The last extension wins.");
    }
}
