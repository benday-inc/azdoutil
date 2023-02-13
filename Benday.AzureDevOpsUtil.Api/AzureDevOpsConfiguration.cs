using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.AzureDevOpsUtil.Api;
public class AzureDevOpsConfiguration
{
    private string _CollectionUrl = string.Empty;

    public string Name { get; set; } = Constants.DefaultConfigurationName;
    public string CollectionUrl
    {
        get
        {
            if (_CollectionUrl.Length > 0)
            {
                if (_CollectionUrl.Trim().EndsWith("/") == false)
                {
                    _CollectionUrl = $"{_CollectionUrl}/";
                }
            }

            return _CollectionUrl;
        }

        set => _CollectionUrl = value;
    }
    public string Token { get; set; } = string.Empty;

    public string GetTokenBase64Encoded()
    {
        var tokenBase64 = Convert.ToBase64String(
            ASCIIEncoding.ASCII.GetBytes(":" + Token));

        return tokenBase64;
    }
}
