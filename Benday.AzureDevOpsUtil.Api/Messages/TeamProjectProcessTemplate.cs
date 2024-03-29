﻿using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class TeamProjectProcessTemplate
{
    [JsonPropertyName("templateTypeId")] 
    public string TemplateTypeId { get; set; } = string.Empty;

    [JsonPropertyName("templateName")]
    public string Name { get; set; } = string.Empty;
}
