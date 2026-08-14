using Benday.AzureDevOpsUtil.Api.TfCommandLine;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class TfExecutableLocatorFixture
{
    /// <summary>
    /// A made-up Windows machine.  Paths are held case-insensitively because
    /// that is how the real thing behaves.
    /// </summary>
    private class FakeFileSystemProbe : IFileSystemProbe
    {
        public HashSet<string> Files { get; } = new(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> Directories { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> EnvironmentVariables { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Registers a file and every folder above it.  Splitting is done on
        /// the backslash rather than with Path.GetDirectoryName, because this
        /// fake stands in for a Windows filesystem no matter which platform the
        /// tests are running on.
        /// </summary>
        public void AddFile(string path)
        {
            Files.Add(path);

            var directory = GetParent(path);

            while (string.IsNullOrWhiteSpace(directory) == false)
            {
                Directories.Add(directory);

                directory = GetParent(directory);
            }
        }

        private static string? GetParent(string path)
        {
            var index = path.LastIndexOf('\\');

            // "C:" on its own is a drive rather than a folder.
            return index <= 2 ? null : path.Substring(0, index);
        }

        public bool FileExists(string path) => Files.Contains(path);

        public bool DirectoryExists(string path) => Directories.Contains(path);

        public IReadOnlyList<string> GetDirectories(string path)
        {
            var prefix = path.TrimEnd('\\') + "\\";

            var children = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var directory in Directories)
            {
                if (directory.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == false)
                {
                    continue;
                }

                var remainder = directory.Substring(prefix.Length);

                var separatorIndex = remainder.IndexOf('\\');

                var name = separatorIndex < 0 ? remainder : remainder.Substring(0, separatorIndex);

                if (name.Length > 0)
                {
                    children.Add(prefix + name);
                }
            }

            return children.ToList();
        }

        public string? GetEnvironmentVariable(string name)
        {
            return EnvironmentVariables.TryGetValue(name, out var value) ? value : null;
        }
    }

    private const string VisualStudio2022Path =
        @"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\TF.exe";

    private static FakeFileSystemProbe BuildProbe()
    {
        var probe = new FakeFileSystemProbe();

        probe.EnvironmentVariables["ProgramFiles"] = @"C:\Program Files";
        probe.EnvironmentVariables["ProgramFiles(x86)"] = @"C:\Program Files (x86)";

        return probe;
    }

    [TestMethod]
    public void Find_LocatesVisualStudio2022()
    {
        var probe = BuildProbe();

        probe.AddFile(VisualStudio2022Path);

        var actual = new TfExecutableLocator(probe).Find();

        Assert.AreEqual(1, actual.Count, "Expected one copy of tf.");
        Assert.AreEqual(VisualStudio2022Path, actual[0].Path, "Wrong path.");
        Assert.AreEqual(
            TfExecutableLocator.SourceVisualStudio, actual[0].Source, "Wrong source.");
        Assert.IsFalse(actual[0].IsOnPath, "This copy is not on the PATH.");
    }

    [TestMethod]
    public void Find_EnumeratesYearsAndEditionsRatherThanGuessingThem()
    {
        // A version released after this code was written should still be found.
        var probe = BuildProbe();

        var future =
            @"C:\Program Files\Microsoft Visual Studio\2031\Ultimate\Common7\IDE\" +
            @"CommonExtensions\Microsoft\TeamFoundation\Team Explorer\TF.exe";

        probe.AddFile(future);

        var actual = new TfExecutableLocator(probe).Find();

        Assert.AreEqual(1, actual.Count, "An unknown year and edition should still be found.");
        Assert.AreEqual(future, actual[0].Path, "Wrong path.");
    }

    [TestMethod]
    public void Find_LocatesSeveralEditionsSideBySide()
    {
        var probe = BuildProbe();

        probe.AddFile(VisualStudio2022Path);

        probe.AddFile(
            @"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\" +
            @"CommonExtensions\Microsoft\TeamFoundation\Team Explorer\TF.exe");

        var actual = new TfExecutableLocator(probe).Find();

        Assert.AreEqual(2, actual.Count, "Both editions should be reported.");
    }

    [TestMethod]
    public void Find_LocatesTheOlderVisualStudioLayout()
    {
        var probe = BuildProbe();

        const string Legacy =
            @"C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\TF.exe";

        probe.AddFile(Legacy);

        var actual = new TfExecutableLocator(probe).Find();

        Assert.AreEqual(1, actual.Count, "Expected one copy of tf.");
        Assert.AreEqual(Legacy, actual[0].Path, "Wrong path.");
        Assert.AreEqual(
            TfExecutableLocator.SourceVisualStudioLegacy, actual[0].Source, "Wrong source.");
    }

    [TestMethod]
    public void Find_LocatesAServerInstall()
    {
        var probe = BuildProbe();

        const string ServerPath =
            @"C:\Program Files\Azure DevOps Server 2022\Tools\TF.exe";

        probe.AddFile(ServerPath);

        var actual = new TfExecutableLocator(probe).Find();

        Assert.AreEqual(1, actual.Count, "Expected one copy of tf.");
        Assert.AreEqual(TfExecutableLocator.SourceServer, actual[0].Source, "Wrong source.");
    }

    [TestMethod]
    public void Find_LocatesACopyOnThePath()
    {
        var probe = BuildProbe();

        var directory = @"C:\tools\tee-clc";

        probe.EnvironmentVariables["PATH"] = directory + ";" + @"C:\Windows\System32";

        probe.AddFile(directory + @"\tf.cmd");

        var actual = new TfExecutableLocator(probe, ';').Find();

        Assert.AreEqual(1, actual.Count, "Expected one copy of tf.");
        Assert.IsTrue(actual[0].IsOnPath, "This copy is on the PATH.");
        Assert.AreEqual(TfExecutableLocator.SourcePath, actual[0].Source, "Wrong source.");
    }

    [TestMethod]
    public void Find_PutsCopiesOnThePathFirst()
    {
        var probe = BuildProbe();

        var directory = @"C:\tools";

        probe.EnvironmentVariables["PATH"] = directory;

        probe.AddFile(directory + @"\TF.exe");
        probe.AddFile(VisualStudio2022Path);

        var actual = new TfExecutableLocator(probe, ';').Find();

        Assert.AreEqual(2, actual.Count, "Both copies should be reported.");
        Assert.IsTrue(
            actual[0].IsOnPath, "A copy that can be run without a full path comes first.");
    }

    [TestMethod]
    public void Find_DoesNotReportTheSameCopyTwice()
    {
        // The Visual Studio folder is on the PATH as well.
        var probe = BuildProbe();

        var directory = VisualStudio2022Path.Substring(
            0, VisualStudio2022Path.LastIndexOf('\\'));

        probe.EnvironmentVariables["PATH"] = directory;

        probe.AddFile(VisualStudio2022Path);

        var actual = new TfExecutableLocator(probe, ';').Find();

        Assert.AreEqual(1, actual.Count, "The same file should be reported once.");
        Assert.IsTrue(actual[0].IsOnPath, "It should be reported as being on the PATH.");
    }

    [TestMethod]
    public void Find_ReturnsNothingOnAMachineWithoutTf()
    {
        var probe = BuildProbe();

        probe.Directories.Add(@"C:\Program Files");

        var actual = new TfExecutableLocator(probe).Find();

        Assert.AreEqual(0, actual.Count, "There is no copy of tf here.");
    }

    [TestMethod]
    public void Find_SurvivesAnEmptyEnvironment()
    {
        var actual = new TfExecutableLocator(new FakeFileSystemProbe()).Find();

        Assert.AreEqual(0, actual.Count, "Nothing to find.");
    }

    [TestMethod]
    public void SplitPathVariable_ReadsEntries()
    {
        var value = @"C:\tools;C:\Windows\System32";

        var actual = TfExecutableLocator.SplitPathVariable(value, ';');

        Assert.AreEqual(2, actual.Count, "Wrong entry count.");
        Assert.AreEqual(@"C:\tools", actual[0], "Wrong first entry.");
    }

    [TestMethod]
    public void SplitPathVariable_IgnoresEmptyEntriesAndQuotes()
    {
        var value = ";\"C:\\tools\";;  ;";

        var actual = TfExecutableLocator.SplitPathVariable(value, ';');

        Assert.AreEqual(1, actual.Count, "Only one entry is real.");
        Assert.AreEqual(@"C:\tools", actual[0], "Quotes should be stripped.");
    }

    [TestMethod]
    public void SplitPathVariable_HandlesNothing()
    {
        Assert.AreEqual(0, TfExecutableLocator.SplitPathVariable(null).Count, "Null is empty.");
        Assert.AreEqual(0, TfExecutableLocator.SplitPathVariable("").Count, "Empty is empty.");
    }
}
