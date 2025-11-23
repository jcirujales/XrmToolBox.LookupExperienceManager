using BulkLookupConfiguration.XrmToolBoxTool.model;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using XrmToolBox.Extensibility;

namespace BulkLookupConfiguration.XrmToolBoxTool.Actions
{
    public static class SolutionActions
    {
        public static void LoadEntitiesFromSolution(
            PluginControlBase control,
            Solution selectedSolution,
            Action<List<EntityMetadata>> onComplete)
        {
            var solutionName = selectedSolution.FriendlyName ?? selectedSolution.UniqueName;

            control.WorkAsync(new WorkAsyncInfo
            {
                Message = $"Loading tables from solution: {solutionName}...",
                Work = (worker, args) =>
                {
                    // Step 1: Get solution components
                    var componentQuery = new QueryExpression("solutioncomponent")
                    {
                        ColumnSet = new ColumnSet("objectid"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("solutionid", ConditionOperator.Equal, selectedSolution.Id),
                                new ConditionExpression("componenttype", ConditionOperator.Equal, 1)
                            }
                        }
                    };

                    var components = control.Service.RetrieveMultiple(componentQuery);
                    var metadataIds = components.Entities
                        .Select(c => c.GetAttributeValue<Guid>("objectid"))
                        .Where(id => id != Guid.Empty)
                        .ToList();

                    if (!metadataIds.Any())
                    {
                        args.Result = new List<EntityMetadata>();
                        return;
                    }

                    // Step 2: Get metadata
                    var request = new RetrieveMetadataChangesRequest
                    {
                        Query = new EntityQueryExpression
                        {
                            Criteria = new MetadataFilterExpression(LogicalOperator.And)
                            {
                                Conditions =
                                {
                                    new MetadataConditionExpression("MetadataId", MetadataConditionOperator.In, metadataIds.ToArray())
                                }
                            },
                            Properties = new MetadataPropertiesExpression
                            {
                                PropertyNames = { "LogicalName", "DisplayName", "SchemaName" }
                            }
                        }
                    };

                    var response = (RetrieveMetadataChangesResponse)control.Service.Execute(request);
                    args.Result = response.EntityMetadata
                        .OrderBy(m => m.DisplayName?.UserLocalizedLabel?.Label ?? m.LogicalName)
                        .ToList();
                },
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show($"Error: {args.Error.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var entities = args.Result as List<EntityMetadata> ?? new List<EntityMetadata>();
                    onComplete(entities);
                }
            });
        }


        public static void LoadSolutions(
            PluginControlBase control,
            Action<List<Solution>> onComplete)
        {
            control.WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading solutions...",
                Work = (worker, args) =>
                {
                    var query = new QueryExpression("solution")
                    {
                        ColumnSet = new ColumnSet("friendlyname", "uniquename", "ismanaged", "version", "solutionid"),
                        Criteria = new FilterExpression
                        {
                            Conditions = { new ConditionExpression("isvisible", ConditionOperator.Equal, true) }
                        },
                        Orders = { new OrderExpression("friendlyname", OrderType.Ascending) }
                    };

                    var result = control.Service.RetrieveMultiple(query);

                    args.Result = result.Entities
                        .Select(e =>
                        {
                            var sol = new Solution();
                            sol.Id = e.Id;
                            sol["uniquename"] = e.GetAttributeValue<string>("uniquename");
                            sol["friendlyname"] = e.GetAttributeValue<string>("friendlyname")
                                                  ?? e.GetAttributeValue<string>("uniquename");
                            sol["version"] = e.GetAttributeValue<string>("version");
                            sol["ismanaged"] = e.GetAttributeValue<bool>("ismanaged");
                            return sol;
                        })
                        .OrderBy(s => s.FriendlyName)
                        .ToList();
                },
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    onComplete((List<Solution>)args.Result);
                }
            });
        }
    }
}

