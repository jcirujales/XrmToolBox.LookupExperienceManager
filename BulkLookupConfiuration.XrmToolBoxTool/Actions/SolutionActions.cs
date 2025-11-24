using BulkLookupConfiguration.XrmToolBoxTool.model;
using BulkLookupConfiguration.XrmToolBoxTool.Services;
using Microsoft.Xrm.Sdk.Metadata;
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
                    var components = DataverseService.GetSolutionCompoonents(selectedSolution.Id, control.Service);
                    var metadataIds = components.Entities
                        .Select(c => c.GetAttributeValue<Guid>("objectid"))
                        .Where(id => id != Guid.Empty)
                        .ToList();

                    if (!metadataIds.Any())
                    {
                        args.Result = new List<EntityMetadata>();
                        return;
                    }
                    var tables = DataverseService.GetTables(metadataIds.ToArray(), control.Service);
                    
                    args.Result = tables
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
                    var solutions = DataverseService.GetSolutions(control.Service);

                    args.Result = solutions.Entities
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

