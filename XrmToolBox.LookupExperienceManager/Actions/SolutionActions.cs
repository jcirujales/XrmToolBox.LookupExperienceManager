using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using XrmToolBox.Extensibility;
using XrmToolBox.LookupExperienceManager.forms;
using XrmToolBox.LookupExperienceManager.Model;
using XrmToolBox.LookupExperienceManager.Properties;
using XrmToolBox.LookupExperienceManager.Services;
using Solution = XrmToolBox.LookupExperienceManager.model.Solution;

namespace XrmToolBox.LookupExperienceManager.Actions
{
    public static class SolutionActions
    {
        public static void OnTargetEntitySelect(
            LookupExperienceManagerControl mainControl,
            IOrganizationService orgService)
        {
            if (mainControl.isSystemUpdate) return; 

            if (mainControl.gridTables.SelectedRows.Count == 0)
            {
                mainControl.gridLookups.DataSource = null;
                UpdateConfigPanel(mainControl);
                mainControl.selectedTable.Text = Resources.DefaultTableSelectionMessage;
                return;
            }

            var logicalName = mainControl.gridTables.SelectedRows[0].Cells["schemaName"].Value?.ToString();
            var displayName = mainControl.gridTables.SelectedRows[0].Cells["displayName"].Value?.ToString();
            mainControl.selectedTable.Text = $"Selected Table: {displayName} - {logicalName}";
            mainControl.selectedTableSchemaName = logicalName;
            if (string.IsNullOrEmpty(logicalName)) return;

            LoadReverseLookupsUsingOneToMany(mainControl, logicalName, orgService);
        }
        private static void LoadReverseLookupsUsingOneToMany(
            LookupExperienceManagerControl mainControl,
            string targetEntityLogicalName,
            IOrganizationService orgService)
        {
            mainControl.WorkAsync(new WorkAsyncInfo
            {
                Message = $"Finding lookups for {targetEntityLogicalName}...",
                Work = (worker, args) =>
                {
                    var relationships = DataverseService.GetOneToManyRelationships(mainControl, targetEntityLogicalName, orgService)
                        .Where(r =>
                            r.ReferencingAttribute != "createdby" &&
                            r.ReferencingAttribute != "createdonbehalfby" &&
                            r.ReferencingAttribute != "modifiedby" &&
                            r.ReferencingAttribute != "modifiedonbehalfby" &&
                            r.ReferencingAttribute != "processinguser" &&
                            r.ReferencingAttribute != "sideloadedpluginownerid" &&
                            r.ReferencingAttribute != "partyid" &&
                            r.ReferencingAttribute != "objectid" &&
                            r.ReferencingAttribute != "owninguser" &&
                            r.ReferencingAttribute != "owningteam" &&
                            r.ReferencingEntity != "socialactivity" // TODO: Find out how to filter out private tables/relationships that are not solution aware
                        )
                        .ToList();

                    var results = DataverseService.GetLookupAttributeInfo(mainControl, relationships, orgService);
                    var lookups = results
                        .OrderBy(r => r.SourceEntity)
                        .ThenBy(r => r.Form)
                        .ThenBy(r => r.Label)
                        .ToList();
                    args.Result = lookups;

                    mainControl.gridLookups.Tag = lookups.Cast<object>().ToList();
                },
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    if (mainControl.selectedTableSchemaName != targetEntityLogicalName)
                    {
                        return; // fixes race condition where user could quickly select a different table with shorter retrieval time
                                // but have its prior selection load from longer retrieval time
                    }

                    var lookups = args.Result;
                    mainControl.gridLookups.Invoke((MethodInvoker)(() =>
                    {
                        mainControl.isSystemUpdate = true;
                        mainControl.gridLookups.DataSource = null;
                        mainControl.gridLookups.DataSource = lookups; 
                        mainControl.gridLookups.ClearSelection();

                        UpdateConfigPanel(mainControl);
                        mainControl.isSystemUpdate = false;
                    }));
                }
            });
        }
        public static void RefreshMetadata(LookupExperienceManagerControl mainControl, IOrganizationService orgService)
        {
            if (mainControl.selectedSolution != null)
            {
                OnSolutionSelected(mainControl, mainControl.selectedSolution);
                if (!string.IsNullOrEmpty(mainControl.selectedTableSchemaName))
                {
                    mainControl.gridTables.Invoke((MethodInvoker)(() =>
                    {
                        // Find and select the row with matching DisplayName or SchemaName
                        foreach (DataGridViewRow row in mainControl.gridTables.Rows)
                        {
                            var displayName = row.Cells["displayName"].Value?.ToString();
                            var schemaName = row.Cells["schemaName"].Value?.ToString();

                            if (schemaName == mainControl.selectedTableSchemaName)
                            {
                                row.Selected = true;
                                mainControl.gridTables.CurrentCell = row.Cells[0];
                                mainControl.gridTables.FirstDisplayedScrollingRowIndex = row.Index;
                                OnTargetEntitySelect(mainControl, orgService);
                                break;
                            }
                        }
                    }));
                }
            }
        }
        public static void OnSolutionSelected(LookupExperienceManagerControl mainControl, Solution selectedSolution)
        {
            var solutionName = selectedSolution.FriendlyName ?? selectedSolution.UniqueName;
            var version = selectedSolution.Version ?? "?.?.?";

            mainControl.Invoke((MethodInvoker)(() =>
            {
                mainControl.lblSelectedSolution.Text = $"Selected: {solutionName} v{version}";
                mainControl.lblSelectedSolution.ForeColor = Color.FromArgb(100, 255, 150);
            }));

            LoadEntitiesFromSolution(mainControl, selectedSolution, entities =>
            {
                mainControl.Invoke((MethodInvoker)(() =>
                {
                    mainControl.isSystemUpdate = true;
                    var tables = entities.Select(entity => new Table
                    {
                        displayName = entity.DisplayName?.UserLocalizedLabel?.Label ?? entity.LogicalName,
                        schemaName = entity.LogicalName
                    }).ToList();

                    if(mainControl.searchBox.Text.ToLower().Trim() != Resources.SearchPlaceholderText.ToLower()) mainControl.searchBox.Clear();

                    mainControl.gridTables.Tag = tables.Cast<object>().ToList(); // store original loaded tables
                    mainControl.gridTables.DataSource = tables;
                    mainControl.gridTables.ClearSelection();
                    mainControl.gridLookups.DataSource = null;
                    UpdateConfigPanel(mainControl);
                    
                    mainControl.isSystemUpdate = false;
                }));
            });
        }
        public static void UpdateConfigPanel(LookupExperienceManagerControl mainControl)
        {
            var selected = mainControl.gridLookups.Rows.Cast<DataGridViewRow>().Where(r => r.Selected).ToList();

            if (mainControl.isSystemUpdate || selected.Count == 0)
            {
                mainControl.gridLookups.ClearSelection();
                mainControl.lblConfigMessage.Text = Resources.DefaultLookupSelectionMessage;
                mainControl.chkDisableNew.Enabled = false;
                mainControl.chkDisableMru.Enabled = false;
                mainControl.chkMainFormCreate.Enabled = false;
                mainControl.chkMainFormEdit.Enabled = false;
                mainControl.btnSavePublish.Visible = false;
                return;
            }

            mainControl.lblConfigMessage.Text = $"Selected: {selected.Count} lookup control{(selected.Count > 1 ? "s" : "")}";

            mainControl.chkDisableNew.Enabled = true;
            mainControl.chkDisableMru.Enabled = true;
            mainControl.chkMainFormCreate.Enabled = true;
            mainControl.chkMainFormEdit.Enabled = true;
            mainControl.btnSavePublish.Visible = true;
            mainControl.btnSavePublish.Text = $"Save and Publish ({selected.Count})";

            SetTriStateCheckBox(mainControl.chkDisableNew, selected, FormXMLAttributes.IsInlineNewEnabled);
            SetTriStateCheckBox(mainControl.chkDisableMru, selected, FormXMLAttributes.DisableMru);
            SetTriStateCheckBox(mainControl.chkMainFormCreate, selected, FormXMLAttributes.UseMainFormDialogForCreate);
            SetTriStateCheckBox(mainControl.chkMainFormEdit, selected, FormXMLAttributes.UseMainFormDialogForEdit);
        }

        private static void SetTriStateCheckBox(CheckBox checkBox, List<DataGridViewRow> selectedRows, string columnName)
        {
            var values = selectedRows.Select(r => (bool)r.Cells[columnName].Value);

            if (values.All(v => v))
                checkBox.CheckState = CheckState.Checked;
            else if (values.Any(v => v))
                checkBox.CheckState = CheckState.Indeterminate;
            else
                checkBox.CheckState = CheckState.Unchecked;
        }
        public static void LoadEntitiesFromSolution(
            LookupExperienceManagerControl mainControl,
            Solution selectedSolution,
            Action<List<EntityMetadata>> onComplete)
        {
            var solutionName = selectedSolution.FriendlyName ?? selectedSolution.UniqueName;

            mainControl.WorkAsync(new WorkAsyncInfo
            {
                Message = $"Loading tables from solution: {solutionName}...",
                Work = (worker, args) =>
                {
                    var components = DataverseService.GetSolutionCompoonents(selectedSolution.Id, mainControl.Service);
                    var metadataIds = components.Entities
                        .Select(c => c.GetAttributeValue<Guid>("objectid"))
                        .Where(id => id != Guid.Empty)
                        .ToList();

                    if (!metadataIds.Any())
                    {
                        args.Result = new List<EntityMetadata>();
                        return;
                    }
                    var tables = DataverseService.GetTables(metadataIds.ToArray(), mainControl.Service);
                    
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

        public static void UnmanagedLayerWarning(LookupExperienceManagerControl mainControl, IOrganizationService orgService)
        {
            var selectedRows = mainControl.gridLookups.SelectedRows.Cast<DataGridViewRow>().ToList();

            if (selectedRows.Count == 0)
                return;

            // Check if any selected form is managed
            var managedForms = selectedRows
                .Where(r => r.Cells["IsManaged"]?.Value is bool b && b)
                .ToList();

            if (managedForms.Any())
            {
                var formList = string.Join("\n• ", managedForms.Select(r =>
                    $"{r.Cells["Form"].Value} ({r.Cells["SourceEntity"].Value})"));

                var result = MessageBox.Show(
                    $"Warning: {managedForms.Count} selected form(s) are MANAGED:\n\n" +
                    $"• {formList}\n\n" +
                    $"Saving changes will create an UNMANAGED layer on top of managed forms.\n" +
                    $"This is usually safe, but cannot be removed later without deleting the layer. It's recommended to create an unmanaged copy of these forms so that they can be updated instead.\n\n" +
                    $"Do you want to continue?",
                    "Managed Forms Detected",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                    return; // User canceled
            }

            SolutionActions.SaveAndPublishCustomizations(mainControl, orgService);
        }
        public static void SaveAndPublishCustomizations(LookupExperienceManagerControl mainControl, IOrganizationService orgService)
        {
            var selectedRows = mainControl.gridLookups.SelectedRows.Cast<DataGridViewRow>().ToList();
            if (selectedRows.Count == 0) return;

            mainControl.WorkAsync(new WorkAsyncInfo
            {
                Message = $"Saving and publishing {selectedRows.Count} lookup control{(selectedRows.Count > 1 ? "s" : "")}...",
                Work = (worker, args) =>
                {
                    var formUpdates = new Dictionary<Guid, XDocument>(); // formId → XDocument (parsed XML)
                    var entitiesToPublish = new HashSet<string>();

                    foreach (DataGridViewRow row in selectedRows)
                    {
                        var schemaName = row.Cells["SchemaName"].Value?.ToString();
                        var sourceEntity = row.Cells["SourceEntity"].Value?.ToString();
                        var formIdStr = row.Cells["FormId"].Value?.ToString();
                        var formXml = row.Cells["FormXml"].Value?.ToString();

                        if (string.IsNullOrEmpty(schemaName) ||
                            string.IsNullOrEmpty(sourceEntity) ||
                            string.IsNullOrEmpty(formIdStr) ||
                            string.IsNullOrEmpty(formXml) ||
                            !Guid.TryParse(formIdStr, out var formId))
                            continue;

                        // Parse once, reuse for all lookups on this form
                        if (!formUpdates.TryGetValue(formId, out var doc))
                        {
                            doc = XDocument.Parse(formXml);
                            formUpdates[formId] = doc;
                        }

                        // Apply this lookup's settings to the shared document
                        UpdateFormXmlLookupSettings(
                            doc,
                            schemaName,
                            mainControl.chkDisableNew.CheckState,
                            mainControl.chkDisableMru.CheckState,
                            mainControl.chkMainFormCreate.CheckState,
                            mainControl.chkMainFormEdit.CheckState);

                        entitiesToPublish.Add(sourceEntity);
                    }

                    // ONE UPDATE PER FORM — elite & fast
                    foreach (var kvp in formUpdates)
                    {
                        var formId = kvp.Key;
                        var updatedXml = kvp.Value.ToString();

                        var formToUpdate = new Entity("systemform", formId)
                        {
                            ["formxml"] = updatedXml
                        };

                        orgService.Update(formToUpdate);
                    }

                    // ONE PUBLISH — all affected entities
                    if (entitiesToPublish.Any())
                    {
                        var publishXml = "<importexportxml><entities>" +
                                         string.Join("", entitiesToPublish.Select(e => $"<entity>{e}</entity>")) +
                                         "</entities></importexportxml>";

                        orgService.Execute(new PublishXmlRequest { ParameterXml = publishXml });
                    }

                    args.Result = selectedRows.Count;
                },
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show($"Error: {args.Error.Message}", "Save Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var count = (int)args.Result;
                    MessageBox.Show($"Successfully updated and published {count} lookup control{(count > 1 ? "s" : "")}!",
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Refresh current view
                    if (mainControl.gridTables.SelectedRows.Count > 0)
                    {
                        var logicalName = mainControl.gridTables.SelectedRows[0].Cells["schemaName"].Value?.ToString();
                        if (!string.IsNullOrEmpty(logicalName))
                            LoadReverseLookupsUsingOneToMany(mainControl, logicalName, orgService);
                    }
                }
            });
        }
        private static void UpdateFormXmlLookupSettings(
            XDocument doc,
            string schemaName,
            CheckState isInlineNewEnabled,
            CheckState disableMru,
            CheckState mainFormCreate,
            CheckState mainFormEdit)
        {
            var controls = doc.Descendants("control")
                .Where(c => c.Attribute("datafieldname")?.Value == schemaName);

            foreach (var control in controls)
            {
                var parameters = control.Element("parameters") ?? new XElement("parameters");
                if (parameters.Parent == null) control.Add(parameters);

                if (isInlineNewEnabled != CheckState.Indeterminate)
                    UpdateParameter(parameters, "IsInlineNewEnabled", isInlineNewEnabled == CheckState.Checked);
                if (disableMru != CheckState.Indeterminate)
                    UpdateParameter(parameters, "DisableMru", disableMru == CheckState.Checked);
                if (mainFormCreate != CheckState.Indeterminate)
                    UpdateParameter(parameters, "useMainFormDialogForCreate", mainFormCreate == CheckState.Checked);
                if (mainFormEdit != CheckState.Indeterminate)
                    UpdateParameter(parameters, "useMainFormDialogForEdit", mainFormEdit == CheckState.Checked);
            }
        }
        private static void UpdateParameter(XElement parameters, string name, bool value)
        {
            var param = parameters.Element(name);
            if (param == null)
            {
                parameters.Add(new XElement(name, value.ToString().ToLower()));
            }
            else if (param.Value != value.ToString().ToLower())
            {
                param.Value = value.ToString().ToLower();
            }
        }
        public static void LoadSolutions(LookupExperienceManagerControl mainControl)
        {
            mainControl.WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading solutions...",
                Work = (worker, args) =>
                {
                    var solutions = DataverseService.GetSolutions(mainControl.Service);

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

                    ShowSolutions(mainControl, (List<Solution>)args.Result);
                }
            });
        }

        private static void ShowSolutions(LookupExperienceManagerControl mainControl, List<Solution> solutions)
        {
            mainControl.BeginInvoke((MethodInvoker)(() =>
            {
                var picker = new SolutionPicker(solutions);
                try
                {
                    if (picker.ShowDialog(mainControl) == DialogResult.OK && picker.SelectedSolution != null)
                    {
                        mainControl.selectedSolution = picker.SelectedSolution;
                        OnSolutionSelected(mainControl, picker.SelectedSolution);
                    }
                }
                finally
                {
                    picker.Dispose();
                }
            }));
        }
    }
}

