using System.Reflection;
using Benday.AzureDevOpsUtil.Api;
using ModelContextProtocol.Server;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class McpToolDocumentationFixture
{
    [TestMethod]
    public void EveryMcpToolIsDocumentedInReadmeTemplates()
    {
        // arrange
        var assembly = typeof(StringUtility).Assembly;

        var toolNames = assembly.GetTypes()
            .Where(type => type.GetCustomAttribute<McpServerToolTypeAttribute>() is not null)
            .SelectMany(type => type.GetMethods(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
            .Select(method => method.GetCustomAttribute<McpServerToolAttribute>()?.Name)
            .Where(name => string.IsNullOrWhiteSpace(name) == false)
            .Select(name => name!)
            .Distinct()
            .Order()
            .ToList();

        Assert.IsTrue(toolNames.Count > 0,
            "Expected to find MCP tools via reflection but found none.");

        var miscDir = MarkdownUsageFormatterFixture.GetPathToMiscDirectory();

        var templateFilenames = new[]
        {
            "readme-mcpserver-github.md",
            "readme-mcpserver-nuget.md"
        };

        foreach (var templateFilename in templateFilenames)
        {
            // act
            var templateContents = File.ReadAllText(
                Path.Combine(miscDir, templateFilename));

            // assert
            foreach (var toolName in toolNames)
            {
                Assert.IsTrue(templateContents.Contains($"`{toolName}`"),
                    $"MCP tool '{toolName}' is not documented in misc/{templateFilename}. " +
                    "Add it to the readme template so the generated READMEs stay in sync.");
            }
        }
    }
}
