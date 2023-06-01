﻿using System.Text;

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
    public bool IsWindowsAuth { get; set; }
    public bool IsAzureDevOpsService
    {
        get
        {
            return CollectionUrl.Contains("dev.azure.com");
        }
    }

    public string AccountNameOrCollectionName
    {
        get
        {
            var uri = new Uri(CollectionUrl);

            var segments = uri.Segments;

            if (segments.Length < 2)
            {
                return string.Empty;
            }
            else
            {
                return segments[1].Replace("/", "");
            }
        }
    }

    public string GetTokenBase64Encoded()
    {
        var tokenBase64 = Convert.ToBase64String(
            ASCIIEncoding.ASCII.GetBytes(":" + Token));

        return tokenBase64;
    }
}