using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.AzureDevOpsUtil.Api;
public class AzureDevOpsConfiguration
{
    public string Name { get; set; } = Constants.DefaultConfigurationName;
    public string CollectionUrl { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
