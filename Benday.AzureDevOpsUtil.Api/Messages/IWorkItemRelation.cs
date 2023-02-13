using System;
using System.Linq;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public interface IWorkItemRelation
{
    string RelationType { get; set; }
    string RelationUrl { get; set; }
}
