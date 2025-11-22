using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System;

namespace BulkLookupConfiguration.XrmToolBoxTool.model
{
    // SolutionRecord.cs
    [EntityLogicalName("solution")]
    public class Solution : Entity
    {
        public Solution() : base("solution") { }

        [AttributeLogicalName("uniquename")]
        public string UniqueName => GetAttributeValue<string>("uniquename");

        [AttributeLogicalName("friendlyname")]
        public string FriendlyName => GetAttributeValue<string>("friendlyname") ?? UniqueName;

        [AttributeLogicalName("version")]
        public string Version => GetAttributeValue<string>("version") ?? "";

        [AttributeLogicalName("ismanaged")]
        public bool IsManaged => GetAttributeValue<bool>("ismanaged");

        public override string ToString()
            => $"{FriendlyName} ({UniqueName}) v{Version}{(IsManaged ? " [Managed]" : "")}";
    }
}


