﻿using Benday.AzureDevOpsUtil.Api;
using Benday.AzureDevOpsUtil.Api.UsageFormatters;
using Benday.CommandsFramework;
namespace Benday.AzureDevOpsUtil.UnitTests;
[TestClass]
public class MarkdownUsageFormatterFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
    }

    private MarkdownUsageFormatter? _SystemUnderTest;

    private MarkdownUsageFormatter SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest = new MarkdownUsageFormatter();
            }

            return _SystemUnderTest;
        }
    }

    [TestMethod]
    public void FormatUsagesAsMarkdown()
    {
        // arrange
        var assembly = typeof(StringUtility).Assembly;

        var usages = new CommandAttributeUtility().GetAllCommandUsages(assembly);

        // act
        var actual = SystemUnderTest.Format(usages, false);

        // assert
        Assert.IsNotNull(actual, "actual markdown was null");
        Assert.IsFalse(string.IsNullOrEmpty(actual), "actual markdown was empty");

        var filename = Path.Combine(Path.GetTempPath(), $"markdown-{DateTime.Now.Ticks}.md");

        Console.WriteLine($"Writing markdown file to {filename}");

        File.WriteAllText(filename, actual);

        // Console.WriteLine($"{actual}");
    }

    [TestMethod]
    public void FormatUsagesAsMarkdown_NoIntraDocumentAnchors()
    {
        // arrange
        var assembly = typeof(StringUtility).Assembly;

        var usages = new CommandAttributeUtility().GetAllCommandUsages(assembly);

        // act
        var actual = SystemUnderTest.Format(usages, true);

        // assert
        Assert.IsNotNull(actual, "actual markdown was null");
        Assert.IsFalse(string.IsNullOrEmpty(actual), "actual markdown was empty");

        var filename = Path.Combine(Path.GetTempPath(), $"markdown-{DateTime.Now.Ticks}.md");

        Console.WriteLine($"Writing markdown file to {filename}");

        File.WriteAllText(filename, actual);

        // Console.WriteLine($"{actual}");
    }

     [TestMethod]
    public void GenerateReadmeFiles()
    {
        // arrange
        var assembly = typeof(CreateBacklogRefinementProcessTemplateCommand).Assembly;

        var usages = new CommandAttributeUtility().GetAllCommandUsages(assembly);

        var solutionDir = GetPathToSolutionRootDirectory();

        Console.WriteLine($"solutionDir: {solutionDir}");

        var miscDir = GetPathToMiscDirectory();

        var pathToGeneratedReadmeFiles = Path.Combine(solutionDir, "generated-readme-files");

        if (Directory.Exists(pathToGeneratedReadmeFiles) == false)
        {
            Directory.CreateDirectory(pathToGeneratedReadmeFiles);
        }

        var readmeHeader = File.ReadAllText(Path.Combine(miscDir, "readme-header.md"));

        var readmeCommandsForNuget = SystemUnderTest.Format(usages, true);
        var readmeCommandsForGitHub = SystemUnderTest.Format(usages, false);

        // act
        string pathToNugetReadme = Path.Combine(pathToGeneratedReadmeFiles, "README-for-nuget.md");
        string pathToGitHubReadme = Path.Combine(pathToGeneratedReadmeFiles, "README.md");

        Console.WriteLine($"pathToNugetReadme: {pathToNugetReadme}");
        Console.WriteLine($"pathToGitHubReadme: {pathToGitHubReadme}");

        File.WriteAllText(pathToNugetReadme,
            readmeHeader + Environment.NewLine + readmeCommandsForNuget
            );

        

        File.WriteAllText(pathToGitHubReadme,
            readmeHeader + Environment.NewLine + readmeCommandsForGitHub
            );
    }

    public static string GetPathToSolutionRootDirectory()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();

        var assemblyPath = assembly.Location;

        var assemblyDir = Path.GetDirectoryName(assemblyPath);

        Console.WriteLine($"assembly dir: {assemblyDir}");

        var relativePathToConfig = "..\\..\\..\\..\\".Replace('\\', Path.DirectorySeparatorChar);

        var pathToDir = Path.Combine(assemblyDir!, relativePathToConfig);

        var dirInfo = new DirectoryInfo(pathToDir);

        Console.WriteLine($"Misc directory: {dirInfo.FullName}");

        Assert.IsTrue(Directory.Exists(pathToDir), "Could not locate directory at '{0}'.",
            pathToDir);

        return dirInfo.FullName;
    }


    public static string GetPathToMiscDirectory()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();

        var assemblyPath = assembly.Location;

        var assemblyDir = Path.GetDirectoryName(assemblyPath);

        Console.WriteLine($"assembly dir: {assemblyDir}");

        var relativePathToConfig = "..\\..\\..\\..\\misc\\".Replace('\\', Path.DirectorySeparatorChar);

        var pathToDir = Path.Combine(assemblyDir!, relativePathToConfig);

        var dirInfo = new DirectoryInfo(pathToDir);

        Console.WriteLine($"Misc directory: {dirInfo.FullName}");

        Assert.IsTrue(Directory.Exists(pathToDir), "Could not locate directory at '{0}' -- '{1}'.",
            pathToDir, new DirectoryInfo(pathToDir).FullName);

        return dirInfo.FullName;
    }
}
