using BulkLookupConfiguration.XrmToolBoxTool.forms;
using BulkLookupConfiguration.XrmToolBoxTool.Model;
using BulkLookupConfiguration.XrmToolBoxTool.Services;
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
using Solution = BulkLookupConfiguration.XrmToolBoxTool.model.Solution;

namespace BulkLookupConfiguration.XrmToolBoxTool.Actions
{
    public static class SolutionActions
    {
        public static void OnTargetEntitySelect(
            BulkLookupConfigurationControl mainControl,
            IOrganizationService orgService)
        {
            if (mainControl.gridTables.SelectedRows.Count == 0)
            {
                mainControl.gridLookups.DataSource = null;
                UpdateConfigPanel(mainControl);
                   
                return;
            }

            var logicalName = mainControl.gridTables.SelectedRows[0].Cells["schemaName"].Value?.ToString();
            if (string.IsNullOrEmpty(logicalName)) return;

            LoadReverseLookupsUsingOneToMany(mainControl, logicalName, orgService);
        }
        private static void LoadReverseLookupsUsingOneToMany(
            BulkLookupConfigurationControl mainControl,
            string targetEntityLogicalName,
            IOrganizationService orgService)
        {
            mainControl.WorkAsync(new WorkAsyncInfo
            {
                Message = $"Finding lookups for {targetEntityLogicalName}...",
                Work = (worker, args) =>
                {
                    var relationships = DataverseService.GetOneToManyRelationships(targetEntityLogicalName, orgService)?.OneToManyRelationships
                        .Where(r =>
                            r.IsCustomizable?.Value == true &&
                            r.IsCustomizable?.CanBeChanged == true &&
                            r.IsValidForAdvancedFind == true &&
                            r.ReferencingAttribute != "createdby" &&
                            r.ReferencingAttribute != "createdonbehalfby" &&
                            r.ReferencingAttribute != "modifiedby" &&
                            r.ReferencingAttribute != "modifiedonbehalfby" &&
                            r.ReferencingAttribute != "processinguser" &&
                            r.ReferencingAttribute != "sideloadedpluginownerid" &&
                            r.ReferencingAttribute != "partyid" &&
                            r.ReferencingAttribute != "owninguser" &&
                            r.ReferencingAttribute != "owningteam"
                        )
                        .ToList();

                    var results = DataverseService.GetLookupAttributeInfo(relationships, orgService);
                    args.Result = results
                        .OrderBy(r => r.SourceEntity)
                        .ThenBy(r => r.Label)
                        .ToList();
                },
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var lookups = (List<LookupInfo>)args.Result;
                    mainControl.gridLookups.Invoke((MethodInvoker)(() =>
                    {
                        mainControl.gridLookups.DataSource = null;
                        mainControl.gridLookups.DataSource = lookups;  // ← Magic: binds by name
                        mainControl.gridLookups.ClearSelection();

                        UpdateConfigPanel(mainControl);
                    }));
                }
            });
        }
        public static void OnSolutionSelected(BulkLookupConfigurationControl mainControl, Solution selectedSolution)
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
                    mainControl.gridTables.ClearSelection();
                    mainControl.gridTables.Rows.Clear();

                    foreach (var entity in entities)
                    {
                        var name = entity.DisplayName?.UserLocalizedLabel?.Label ?? entity.LogicalName;
                        mainControl.gridTables.Rows.Add(name, entity.LogicalName);
                    }

                    mainControl.gridLookups.DataSource = null;
                    UpdateConfigPanel(mainControl);
                }));
            });
        }

        public static void UpdateConfigPanel(BulkLookupConfigurationControl mainControl)
        {
            var selected = mainControl.gridLookups.Rows.Cast<DataGridViewRow>().Where(r => r.Selected).ToList();

            if (selected.Count == 0)
            {
                mainControl.lblConfigMessage.Text = "Selected: 0 lookup controls";
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

            SetTriStateCheckBox(mainControl.chkDisableNew, selected, "IsInlineNewEnabled");
            SetTriStateCheckBox(mainControl.chkDisableMru, selected, "DisableMru");
            SetTriStateCheckBox(mainControl.chkMainFormCreate, selected, "UseMainFormDialogForCreate");
            SetTriStateCheckBox(mainControl.chkMainFormEdit, selected, "UseMainFormDialogForEdit");
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
            BulkLookupConfigurationControl mainControl,
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
        public static void SaveAndPublishCustomizations(BulkLookupConfigurationControl mainControl, IOrganizationService orgService)
        {
            var selectedRows = mainControl.gridLookups.SelectedRows.Cast<DataGridViewRow>().ToList();
            if (selectedRows.Count == 0)
                return;

            mainControl.WorkAsync(new WorkAsyncInfo
            {
                Message = $"Saving and publishing {selectedRows.Count} form{(selectedRows.Count > 1 ? "s" : "")}...",
                Work = (worker, args) =>
                {
                    var modifiedForms = new HashSet<string>(); // Track form IDs to publish

                    foreach (DataGridViewRow row in selectedRows)
                    {
                        var schemaName = row.Cells["SchemaName"].Value?.ToString();
                        var sourceEntity = row.Cells["SourceEntity"].Value?.ToString();
                        var formName = row.Cells["Form"].Value?.ToString();
                        var formId = new Guid(row.Cells["FormId"].Value?.ToString());
                        var formXml = row.Cells["FormXml"].Value?.ToString();

                        if (string.IsNullOrEmpty(schemaName) || string.IsNullOrEmpty(sourceEntity))
                            continue;

                        var formToUpdate = new Entity("systemform", formId)
                        {
                            ["formxml"] = formXml
                        };

                        if (string.IsNullOrEmpty(formXml)) continue;

                        // Parse and update XML
                        var updatedXml = UpdateFormXmlLookupSettings(
                            formXml,
                            schemaName,
                            mainControl.chkDisableNew.CheckState,
                            mainControl.chkDisableMru.CheckState,
                            mainControl.chkMainFormCreate.CheckState,
                            mainControl.chkMainFormEdit.CheckState
                        );

                        if (updatedXml != formXml)
                        {
                            // Save updated form
                            formToUpdate["formxml"] = updatedXml;
                            orgService.Update(formToUpdate);
                            modifiedForms.Add(sourceEntity); // Publish entity
                        }
                    }

                    // Publish all modified entities
                    foreach (var entity in modifiedForms)
                    {
                        var request = new PublishXmlRequest
                        {
                            ParameterXml = $"<importexportxml><entities><entity>{entity}</entity></entities></importexportxml>"
                        };
                        orgService.Execute(request);
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
                    MessageBox.Show($"Successfully saved and published {count} form{(count > 1 ? "s" : "")}!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Optional: refresh grid
                    if (mainControl.gridTables.SelectedRows.Count > 0)
                    {
                        var logicalName = mainControl.gridTables.SelectedRows[0].Cells["schemaName"].Value?.ToString();
                        if (!string.IsNullOrEmpty(logicalName))
                            LoadReverseLookupsUsingOneToMany(mainControl, logicalName, orgService);
                    }
                }
            });
        }
        private static string UpdateFormXmlLookupSettings(
            string formXml,
            string schemaName,
            CheckState isInlineNewEnabled,
            CheckState disableMru,
            CheckState mainFormCreate,
            CheckState mainFormEdit)
        {
            var doc = XDocument.Parse(formXml);
            var changed = false;

            var controls = doc.Descendants("control")
                .Where(c => c.Attribute("datafieldname")?.Value == schemaName);

            foreach (var control in controls)
            {
                var parameters = control.Element("parameters") ?? new XElement("parameters");
                if (parameters.Parent == null) control.Add(parameters);

                var settings = new[]
                    {
                        ("IsInlineNewEnabled", isInlineNewEnabled),
                        ("DisableMru", disableMru),
                        ("useMainFormDialogForCreate", mainFormCreate),
                        ("useMainFormDialogForEdit", mainFormEdit)
                    };
                foreach (var (name, state) in settings)
                {
                    if (state != CheckState.Indeterminate)
                    {
                        UpdateParameter(parameters, name, state == CheckState.Checked);
                        changed = true;
                    }
                }
            }

            return changed ? doc.ToString() : formXml;
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
        public static void LoadSolutions(BulkLookupConfigurationControl mainControl)
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

        private static void ShowSolutions(BulkLookupConfigurationControl mainControl, List<Solution> solutions)
        {
            mainControl.BeginInvoke((MethodInvoker)(() =>
            {
                var picker = new SolutionPicker(solutions);
                try
                {
                    if (picker.ShowDialog(mainControl) == DialogResult.OK && picker.SelectedSolution != null)
                    {
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

