using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.AzureDevOpsUtil.Api.ScriptGenerator;
public class WorkItemScriptSprint
{
    public int SprintNumber { get; set; }
    public int NewPbiCount { get; set; }
    public int RefinedPbiCountMeeting1 { get; set; }
    public int RefinedPbiCountMeeting2 { get; set; }
    public int SprintPbiCount { get; set; }
    public int SprintPbisToDoneCount { get; set; }
    public int AverageNumberOfTasksPerPbi { get; set; }
}
