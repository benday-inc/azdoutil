
using System.Diagnostics;
using System.Reflection.PortableExecutable;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

using OfficeOpenXml.Utils;

using Benday.AzureDevOpsUtil.Api.Commands.ProjectAdministration;
using Benday.AzureDevOpsUtil.Api.Commands.WorkItems;
namespace Benday.AzureDevOpsUtil.Api.Commands.ProcessTemplates;

[Command(
    Category = Constants.Category_WorkItems,
    Name = Constants.CommandArgumentNameExportProjectProcessTemplate,
    Description = "Exports the process template configuration for one or more projects. This command only works on Windows and requires witadmin.exe to be installed.",
    IsAsync = true)]
public class ExportProjectProcessTemplateCommand : AzureDevOpsCommandBase
{
    public ExportProjectProcessTemplateCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();


        AddCommonArguments(args);

        args.AddString(Constants.ArgumentNameTeamProjectName).
            AsNotRequired().
            WithDescription("Team project name to export.");

        args.AddBoolean(Constants.ArgumentNameAllProjects).
            AsNotRequired().
            WithDefaultValue(false).
            AllowEmptyValue().
            WithDescription("Export all projects in the organization or team project collection.");

        args.AddString(Constants.ArgumentNameExportToPath).
            AsNotRequired().
            WithDefaultValue(string.Empty).
            WithDescription("Path to export the process template to.  If not specified, the current directory is used.");

        args.AddString(Constants.ArgumentNamePathToWitAdminExe).
            AsNotRequired().
            WithDescription("Specify path to witadmin.exe if it can't be located automatically.");

        return args;
    }

    protected override async Task OnExecute()
    {
        if (OperatingSystem.IsWindows() == false)
        {
            throw new KnownException("This command is only supported on Windows.");
        }

        var pathToWitAdminExe = GetPathToWitAdminExe();

        var exportAllProjects = Arguments.GetBooleanValue(Constants.ArgumentNameAllProjects);

        if (Arguments.HasValue(Constants.ArgumentNameTeamProjectName) == true &&
            exportAllProjects == true)
        {
            throw new KnownException("You cannot specify both the team project name and the all projects flag.");
        }
        else if (Arguments.HasValue(Constants.ArgumentNameTeamProjectName) == false &&
            exportAllProjects == false)
        {
            throw new KnownException("You must specify either the team project name or the all projects flag.");
        }
        
        var projectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        
        var outputFolder = Arguments.GetPathToDirectory(Constants.ArgumentNameExportToPath, true);

        if (exportAllProjects == true)
        {
            await ExportAllProjects(pathToWitAdminExe, outputFolder);
        }
        else
        {
            await ExportProject(pathToWitAdminExe, projectName, outputFolder);
        }
    }

    private async Task<List<string>> GetWorkItemTypes(string project)
    {
        var args = ExecutionInfo.GetCloneOfArguments(
                        Constants.CommandArgumentNameGetWorkItemTypes,
                        true);

        args.AddArgumentValue(Constants.ArgumentNameNameOnly, true.ToString());
        args.AddArgumentValue(Constants.ArgumentNameTeamProjectName, project);

        var command = new GetWorkItemTypesCommand(
            args, _OutputProvider);

        await command.ExecuteAsync();

        if (command.AllWorkItemTypes == null)
        {
            throw new KnownException($"Could not get work item types.");
        }

        var returnValue = new List<string>();

        foreach (var wit in command.AllWorkItemTypes.Types)
        {
            returnValue.Add(wit.Name);
        }

        return returnValue;
    }

    private async Task ExportAllProjects(
        FileInfo pathToWitAdminExe, string outputFolder)
    {
        // get all projects
      
        var args = ExecutionInfo.GetCloneOfArguments(
                        Constants.CommandName_ListProjects,
                        true);        

        var command = new ListTeamProjectsCommand(
            args, _OutputProvider);

        await command.ExecuteAsync();

        if (command.LastResult == null)
        {
            throw new KnownException($"Could not team projects.");
        }

        foreach (var project in command.LastResult.Projects)
        {
            await ExportProject(pathToWitAdminExe, project.Name, outputFolder);
        }
    }

    private async Task ExportProject(
        FileInfo pathToWitAdminExe, string project, string exportBasePath)
    {
        WriteLine($"Exporting process template for project {project}");

        WriteLine($"Creating export directory for project {project}");

        string exportDirForProject = Path.Combine(exportBasePath, project);

        // Create the export directory if it doesn't exist
        if (!Directory.Exists(exportDirForProject))
        {
            Directory.CreateDirectory(exportDirForProject);
        }

        WriteLine($"Exporting process template for project {project} to {exportDirForProject}");

        WriteLine("Getting work item types for project...");

        // Get the work item types for the project
        var witTypes = await GetWorkItemTypes(project);

        string typeDefDir = Path.Combine(exportDirForProject, "TypeDefinitions");

        if (!Directory.Exists(typeDefDir))
        {
            Directory.CreateDirectory(typeDefDir);
        }

        var tpcUrl = this.Configuration.CollectionUrl;

        // Loop through the work item types
        foreach (var witType in witTypes)
        {
            WriteLine($"Exporting work item type {witType} for project {project}");
            string typeDefOutputFile = Path.Combine(typeDefDir, $"{witType}.xml");

            RunWitAdmin(pathToWitAdminExe, $"exportwitd /collection:\"{tpcUrl}\" /p:\"{project}\" /n:\"{witType}\" /f:\"{typeDefOutputFile}\"");
        }

        string processConfigDir = Path.Combine(exportDirForProject, "Process");

        if (!Directory.Exists(processConfigDir))
        {
            Directory.CreateDirectory(processConfigDir);
        }

        WriteLine($"Exporting process config for project {project}");
        string processConfigFile = Path.Combine(processConfigDir, "processconfig.xml");
        RunWitAdmin(pathToWitAdminExe, $"exportprocessconfig /collection:\"{tpcUrl}\" /p:\"{project}\" /f:\"{processConfigFile}\"");

        WriteLine($"Exporting categories for project {project}");
        string categoriesFile = Path.Combine(exportDirForProject, "categories.xml");
        RunWitAdmin(pathToWitAdminExe, $"exportcategories /collection:\"{tpcUrl}\" /p:\"{project}\" /f:\"{categoriesFile}\"");

        WriteLine("Exporting global list for TPC...");
        string globalListFile = Path.Combine(exportDirForProject, "globallist.xml");
        RunWitAdmin(pathToWitAdminExe, $"exportgloballist /collection:\"{tpcUrl}\" /f:\"{globalListFile}\"");

        WriteLine("Exporting global workflow for TPC...");
        string globalWorkflowFile = Path.Combine(exportDirForProject, "globalworkflow.xml");
        RunWitAdmin(pathToWitAdminExe, $"exportglobalworkflow /collection:\"{tpcUrl}\" /f:\"{globalWorkflowFile}\"");

        // Get link types
        WriteLine($"Getting link types for project {project}...");
        List<string> linkTypes =
            RunWitAdmin(pathToWitAdminExe, $"listlinktypes /collection:\"{tpcUrl}\"");

        string linkTypesDir = Path.Combine(exportDirForProject, "LinkTypes");

        if (!Directory.Exists(linkTypesDir))
        {
            Directory.CreateDirectory(linkTypesDir);
        }

        foreach (var linkType in linkTypes)
        {
            // If link type starts with "Reference Name: " then skip
            if (linkType.StartsWith("Reference Name: "))
            {
                string linkTypeName = linkType.Replace("Reference Name: ", "");
                WriteLine($"Exporting link type {linkTypeName} for project {project}");
                string linkTypeFile = Path.Combine(linkTypesDir, $"{linkTypeName}.xml");
                RunWitAdmin(pathToWitAdminExe, 
                    $"exportlinktype /collection:\"{tpcUrl}\" /n:\"{linkTypeName}\" /f:\"{linkTypeFile}\"");
            }
        }

        WriteLine($"Done exporting process template for project {project}");
    }

    private List<string> RunWitAdmin(FileInfo pathToWitAdminExe, string arguments)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = pathToWitAdminExe.FullName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);

        if (process != null)
        {
            process.WaitForExit();

            var exitCode = process.ExitCode;

            if (exitCode != 0)
            {
                // read the error output
                using var reader = process.StandardError;
                string error = reader.ReadToEnd();
                WriteLine(error);
                throw new KnownException($"witadmin.exe exited with code {exitCode}.");
            }
            else
            {
                using var reader = process.StandardOutput;
                string result = reader.ReadToEnd();

                var returnValues = result.Split(Environment.NewLine);

                return returnValues.ToList();
            }
        }
        else
        {
            throw new KnownException($"witadmin.exe not found at path '{pathToWitAdminExe.FullName}'.");
        }
    }

    private FileInfo GetPathToWitAdminExe()
    {
        if (Arguments.HasValue(Constants.ArgumentNamePathToWitAdminExe) == true)
        {
            // wit admin path has been specified on the command line
            var info = new FileInfo(
                Arguments.GetStringValue(Constants.ArgumentNamePathToWitAdminExe));

            if (info.Exists == false)
            {
                throw new KnownException($"witadmin.exe not found at path '{info.FullName}'.");
            }
            else
            {
                return info;
            }
        }
        else
        {
            var path = GetWitAdminUsingPowershellGetCommand();

            if (path != null)
            {
                return new FileInfo(path);
            }

            path = GetWitAdminUsingLikelyLocations();

            if (path != null)
            {
                return new FileInfo(path);
            }
            else
            {
                throw new KnownException($"witadmin.exe not found.  Please specify the path to witadmin.exe using the {Constants.ArgumentNamePathToWitAdminExe} argument.");
            }
        }
    }

    private string? GetWitAdminUsingLikelyLocations()
    {
        // get location of program files directory
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        var editions = new string[] { "Enterprise", "Professional", "Community" };
        var versionYears = new string[] { "2022", "2019" };

        string? path = null;

        foreach (var year in versionYears)
        {
            foreach (var edition in editions)
            {
                path = GetWitAdminInVisualStudioDir(programFiles, edition, year);

                if (path != null)
                {
                    return path;
                }
            }
        }

        return null;
    }

    private string? GetWitAdminInVisualStudioDir(string programFiles, string edition, string versionYear)
    {
        var path = Path.Combine(programFiles, "Microsoft Visual Studio", versionYear, edition, "Common7", "IDE", "witadmin.exe");

        // if witadmin.exe exists, return the path
        if (File.Exists(path) == true)
        {
            var info = new FileInfo(path);

            return info.FullName;
        }
        else
        {
            return null;
        }
    }

    private string? GetWitAdminUsingPowershellGetCommand()
    {
        // make a call to powershell to get the path to witadmin.exe

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "Get-Command witadmin.exe | Select-Object -ExpandProperty Source",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };


        using var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();

        process.WaitForExit();

        var path = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        if (string.IsNullOrWhiteSpace(error) == false)
        {
            return null;
        }
        else
        {
            var returnValue = new FileInfo(path);

            return returnValue.FullName;
        }
    }
}
