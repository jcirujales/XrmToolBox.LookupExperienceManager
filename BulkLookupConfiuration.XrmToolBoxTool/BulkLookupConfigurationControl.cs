using BulkLookupConfiguration.XrmToolBoxTool.Actions;
using BulkLookupConfiguration.XrmToolBoxTool.forms;
using BulkLookupConfiguration.XrmToolBoxTool.model;
using BulkLookupConfiguration.XrmToolBoxTool.Services;
using McTools.Xrm.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using XrmToolBox.Extensibility;
using Label = System.Windows.Forms.Label;

namespace BulkLookupConfiguration.XrmToolBoxTool
{
    public partial class BulkLookupConfigurationControl : PluginControlBase
    {
        private Settings mySettings;
        private Label lblSelectedSolution;
        private Label lblConfigMessage;
        private CheckBox chkDisableNew;
        private CheckBox chkDisableMru;
        private CheckBox chkMainFormCreate;
        private CheckBox chkMainFormEdit;
        private Button btnSavePublish;

        public DataGridView GridTables { get; private set; }
        public DataGridView GridLookups { get; private set; }

        public BulkLookupConfigurationControl()
        {
            SetupPluginControl();
            SetupModernLayout();

            this.Load += BulkLookupConfigurationControl_Load;
            this.OnCloseTool += BulkLookupConfigurationControl_OnCloseTool;
        }

        private void SetupPluginControl()
        {
            this.Dock = DockStyle.Fill;
            this.Margin = new Padding(0);

            var statusPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(30, 34, 42),
                Padding = new Padding(12, 0, 12, 0)
            };

            var lblTitle = new Label
            {
                Text = "Lookup Experience Manager",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                AutoSize = true,
                Dock = DockStyle.Left,
                Padding = new Padding(0, 8, 0, 0)
            };

            lblSelectedSolution = new Label
            {
                Text = "No solution selected",
                ForeColor = Color.FromArgb(180, 200, 255),
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Dock = DockStyle.Right,
                Padding = new Padding(0, 10, 20, 0)
            };

            statusPanel.Controls.Add(lblTitle);
            statusPanel.Controls.Add(lblSelectedSolution);

            var toolbar = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                BackColor = Color.FromArgb(40, 44, 52),
                ForeColor = Color.White,
                Renderer = new ToolStripProfessionalRenderer(new CustomProfessionalColors())
            };

            var btnClose = new ToolStripButton("Close Tool")
            {
                Alignment = ToolStripItemAlignment.Right
            };
            btnClose.Click += (s, e) => CloseTool();

            var btnSolutions = new ToolStripButton("Select Solution")
            {
                Image = Properties.Resources.Solutions_32,
                ImageScaling = ToolStripItemImageScaling.None,
                ToolTipText = "Select a solution to analyze"
            };
            btnSolutions.Click += tsb_opensolutions_Click;

            var btnSample = new ToolStripButton("Sample Query")
            {
                ToolTipText = "Run sample account query"
            };
            btnSample.Click += tsbSample_Click;

            toolbar.Items.Add(btnSolutions);
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(btnSample);
            toolbar.Items.Add(new ToolStripSeparator { Margin = new Padding(20, 0, 20, 0) });
            toolbar.Items.Add(btnClose);

            this.Controls.Add(toolbar);
            this.Controls.Add(statusPanel);
        }

        private void OnSolutionSelected(Solution selectedSolution)
        {
            var solutionName = selectedSolution.FriendlyName ?? selectedSolution.UniqueName;
            var version = selectedSolution.Version ?? "?.?.?";

            this.Invoke((MethodInvoker)(() =>
            {
                lblSelectedSolution.Text = $"Selected: {solutionName} v{version}";
                lblSelectedSolution.ForeColor = Color.FromArgb(100, 255, 150);
            }));

            SolutionActions.LoadEntitiesFromSolution(this, selectedSolution, entities =>
            {
                GridTables.Invoke((MethodInvoker)(() =>
                {
                    GridTables.ClearSelection();
                    GridTables.Rows.Clear();

                    foreach (var entity in entities)
                    {
                        var name = entity.DisplayName?.UserLocalizedLabel?.Label ?? entity.LogicalName;
                        GridTables.Rows.Add(name, entity.LogicalName);
                    }

                    GridLookups.DataSource = null;
                    UpdateConfigPanel();
                }));
            });
        }

        private void GridTables_SelectionChanged(object sender, EventArgs e)
        {
            if (GridTables.SelectedRows.Count == 0)
            {
                GridLookups.DataSource = null;
                UpdateConfigPanel();
                return;
            }

            var logicalName = GridTables.SelectedRows[0].Cells["schemaName"].Value?.ToString();
            if (string.IsNullOrEmpty(logicalName)) return;

            LoadReverseLookupsUsingOneToMany(logicalName);
        }

        private void LoadReverseLookupsUsingOneToMany(string targetEntityLogicalName)
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = $"Finding lookups for {targetEntityLogicalName}...",
                Work = (worker, args) =>
                {
                    var relationships = DataverseService.GetOneToManyRelationships(targetEntityLogicalName, Service)?.OneToManyRelationships
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

                    var results = DataverseService.GetLookupAttributeInfo(relationships, Service);
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
                    GridLookups.Invoke((MethodInvoker)(() =>
                    {
                        GridLookups.DataSource = null;
                        GridLookups.DataSource = lookups;  // ← Magic: binds by name
                        GridLookups.ClearSelection();

                        UpdateConfigPanel();
                    }));
                }
            });
        }

        private void UpdateConfigPanel()
        {
            var selected = GridLookups.Rows.Cast<DataGridViewRow>().Where(r => r.Selected).ToList();

            if (selected.Count == 0)
            {
                lblConfigMessage.Text = "Selected: 0 lookup controls";
                chkDisableNew.Enabled = false;
                chkDisableMru.Enabled = false;
                chkMainFormCreate.Enabled = false;
                chkMainFormEdit.Enabled = false;
                btnSavePublish.Visible = false;
                return;
            }

            lblConfigMessage.Text = $"Selected: {selected.Count} lookup control{(selected.Count > 1 ? "s" : "")}";

            chkDisableNew.Enabled = true;
            chkDisableMru.Enabled = true;
            chkMainFormCreate.Enabled = true;
            chkMainFormEdit.Enabled = true;
            btnSavePublish.Visible = true;
            btnSavePublish.Text = $"Save and Publish ({selected.Count})";

            SetTriStateCheckBox(chkDisableNew, selected, "IsInlineNewEnabled");
            SetTriStateCheckBox(chkDisableMru, selected, "DisableMru");
            SetTriStateCheckBox(chkMainFormCreate, selected, "UseMainFormDialogForCreate");
            SetTriStateCheckBox(chkMainFormEdit, selected, "UseMainFormDialogForEdit");
        }

        private void SetTriStateCheckBox(CheckBox checkBox, List<DataGridViewRow> selectedRows, string columnName)
        {
            var values = selectedRows.Select(r => (bool)r.Cells[columnName].Value);

            if (values.All(v => v))
                checkBox.CheckState = CheckState.Checked;
            else if (values.Any(v => v))
                checkBox.CheckState = CheckState.Indeterminate;
            else
                checkBox.CheckState = CheckState.Unchecked;
        }

        public class LookupInfo
        {
            public string SourceEntity { get; set; }
            public string Form { get; set; }
            public Guid FormId { get; set; }
            public string FormXml{ get; set; }
            public string Label { get; set; }
            public string SchemaName { get; set; }
            public bool IsInlineNewEnabled { get; set; }
            public bool DisableMru { get; set; }
            public bool UseMainFormDialogForEdit { get; set; }
            public bool UseMainFormDialogForCreate { get; set; }
        }

        private class CustomProfessionalColors : ProfessionalColorTable
        {
            public override Color MenuItemSelected => Color.FromArgb(0, 122, 204);
            public override Color ToolStripDropDownBackground => Color.FromArgb(45, 50, 60);
            public override Color ImageMarginGradientBegin => Color.FromArgb(45, 50, 60);
            public override Color ImageMarginGradientMiddle => Color.FromArgb(45, 50, 60);
            public override Color ImageMarginGradientEnd => Color.FromArgb(45, 50, 60);
        }

        private void BulkLookupConfigurationControl_Load(object sender, EventArgs e)
        {
            LoadSettings();
            ExecuteMethod(() => DataverseService.WhoAmI(Service));
        }

        private void LoadSettings()
        {
            ShowInfoNotification("Welcome to Lookup Experience Manager", new Uri("https://github.com/MscrmTools/XrmToolBox"));
            if (!SettingsManager.Instance.TryLoad(GetType(), out mySettings))
            {
                mySettings = new Settings();
                LogWarning("Settings not found => a new settings file has been created!");
            }
            else
            {
                LogInfo("Settings loaded successfully");
            }
        }

        private void tsbSample_Click(object sender, EventArgs e) => ExecuteMethod(GetAccounts);

        private void GetAccounts()
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Getting accounts",
                Work = (worker, args) =>
                {
                    args.Result = Service.RetrieveMultiple(new QueryExpression("account") { TopCount = 50 });
                },
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                        MessageBox.Show(args.Error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else if (args.Result is EntityCollection ec)
                        MessageBox.Show($"Found {ec.Entities.Count} accounts");
                }
            });
        }

        /// <summary>
        /// This event occurs when the plugin is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BulkLookupConfigurationControl_OnCloseTool(object sender, EventArgs e)
        {
            // Before leaving, save the settings
            SettingsManager.Instance.Save(GetType(), mySettings);
        }

        /// <summary>
        /// This event occurs when the connection has been updated in XrmToolBox
        /// </summary>
        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            if (mySettings != null && detail != null)
            {
                mySettings.LastUsedOrganizationWebappUrl = detail.WebApplicationUrl;
                LogInfo("Connection updated to: {0}", detail.WebApplicationUrl);
            }
        }

        private void tsb_opensolutions_Click(object sender, EventArgs e)
        {
            SolutionActions.LoadSolutions(this, solutions =>
            {
                this.BeginInvoke((MethodInvoker)(() =>
                {
                    var picker = new SolutionPicker(solutions);
                    try
                    {
                        if (picker.ShowDialog(this) == DialogResult.OK && picker.SelectedSolution != null)
                        {
                            OnSolutionSelected(picker.SelectedSolution);
                        }
                    }
                    finally
                    {
                        picker.Dispose();
                    }
                }));
            });
        }

        private void btnSavePublish_Click(object sender, EventArgs e)
        {
            var selectedRows = GridLookups.SelectedRows.Cast<DataGridViewRow>().ToList();
            if (selectedRows.Count == 0)
                return;

            WorkAsync(new WorkAsyncInfo
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
                            chkDisableNew.CheckState,
                            chkDisableMru.CheckState,
                            chkMainFormCreate.CheckState,
                            chkMainFormEdit.CheckState
                        );

                        if (updatedXml != formXml)
                        {
                            // Save updated form
                            formToUpdate["formxml"] = updatedXml;
                            Service.Update(formToUpdate);
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
                        Service.Execute(request);
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
                    if (GridTables.SelectedRows.Count > 0)
                    {
                        var logicalName = GridTables.SelectedRows[0].Cells["schemaName"].Value?.ToString();
                        if (!string.IsNullOrEmpty(logicalName))
                            LoadReverseLookupsUsingOneToMany(logicalName);
                    }
                }
            });
        }

        private string UpdateFormXmlLookupSettings(
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

        private void UpdateParameter(XElement parameters, string name, bool value)
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

        private void SetupModernLayout()
        {
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(0),
                BackColor = Color.FromArgb(40, 44, 52)
            };

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            // LEFT: Target Entity
            var panelTables = CreateModernPanel("Target Entity", "Select a target entity to see all lookup controls pointing to it");
            GridTables = CreateStyledGrid();
            GridTables.SelectionChanged += GridTables_SelectionChanged;
            GridTables.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { HeaderText = "Display Name", FillWeight = 55, Name = "displayName", DataPropertyName="displayName" },
                new DataGridViewTextBoxColumn { HeaderText = "Schema Name", FillWeight = 45, Name = "schemaName", DataPropertyName="schemaName" }
            });
            panelTables.Controls.Add(GridTables);
            panelTables.Controls.SetChildIndex(GridTables, 0);

            // MIDDLE: Lookup Controls
            var panelLookups = CreateModernPanel("Lookup Controls", "Select lookup fields to configure");
           
            GridLookups = CreateStyledGrid();
            GridLookups.SelectionChanged += GridLookups_SelectionChanged;
            GridLookups.MultiSelect = true;
            GridLookups.AutoGenerateColumns = false; // ← CRITICAL

            GridLookups.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn
                {
                    HeaderText = "Source Entity",
                    Name = "SourceEntity",
                    DataPropertyName="SourceEntity",
                    FillWeight = 25
                },
                new DataGridViewTextBoxColumn
                {
                    HeaderText = "Form",
                    Name = "Form",
                    DataPropertyName="Form",
                    FillWeight = 25
                },
                new DataGridViewTextBoxColumn
                {
                    HeaderText = "Form ID",
                    Name = "FormId",
                    DataPropertyName="FormId",
                    Visible = false,
                    FillWeight = 25
                },
                new DataGridViewTextBoxColumn
                {
                    HeaderText = "Form XML",
                    Name = "FormXml",
                    DataPropertyName="FormXml",
                    Visible = false,
                    FillWeight = 25
                },
                new DataGridViewTextBoxColumn
                {
                    HeaderText = "Label",
                    Name = "Label",
                    DataPropertyName="Label",
                    FillWeight = 25
                },
                new DataGridViewTextBoxColumn
                {
                    HeaderText = "Schema Name",
                    Name = "SchemaName",
                    DataPropertyName="SchemaName",
                    FillWeight = 25
                },
                new DataGridViewCheckBoxColumn
                {
                    HeaderText = "Disable MRU",
                    Name = "DisableMru",
                    DataPropertyName="DisableMru",
                    FillWeight = 20
                },
                new DataGridViewCheckBoxColumn
                {
                    HeaderText = "+ New",
                    Name = "IsInlineNewEnabled",
                    DataPropertyName="IsInlineNewEnabled",
                    FillWeight = 20
                },
                new DataGridViewCheckBoxColumn
                {
                    HeaderText = "Main Form (Create)",
                    Name = "useMainFormDialogForCreate",
                    DataPropertyName="useMainFormDialogForCreate",
                    FillWeight = 20
                },
                new DataGridViewCheckBoxColumn
                {
                    HeaderText = "Main Form (Edit)",
                    Name = "useMainFormDialogForEdit",
                    DataPropertyName="useMainFormDialogForEdit",
                    FillWeight = 20
                },
            });
            panelLookups.Controls.Add(GridLookups);
            panelLookups.Controls.SetChildIndex(GridLookups, 0);

            // RIGHT: Configuration — CLEAN & ELEGANT
            var panelConfig = new Panel { Dock = DockStyle.Fill };

            var headerConfig = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(0, 122, 204),
                Padding = new Padding(16, 12, 16, 12)
            };

            var lblConfigTitle = new Label
            {
                Text = "Configuration",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 13F),
                AutoSize = true,
                Location = new Point(0, 0)
            };

            var lblConfigSubtitle = new Label
            {
                Text = "Select one or more lookup controls to configure",
                ForeColor = Color.FromArgb(220, 240, 255),
                Font = new Font("Segoe UI", 9F),
                AutoSize = false,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 28, 0, 0)
            };

            headerConfig.Controls.Add(lblConfigSubtitle);
            headerConfig.Controls.Add(lblConfigTitle);
            lblConfigTitle.BringToFront();

            var content = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                WrapContents = false
            };

            lblConfigMessage = new Label
            {
                Text = "Selected: 0 lookup controls",
                ForeColor = Color.FromArgb(180, 180, 255),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 20)
            };

            chkDisableNew = new CheckBox
            {
                Text = "Enable + New button",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Enabled = false,
                Margin = new Padding(0, 0, 0, 12)
            };

            chkDisableMru = new CheckBox
            {
                Text = "Hide Recently Used (MRU)",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Enabled = false,
                Margin = new Padding(0, 0, 0, 12)
            };

            chkMainFormCreate = new CheckBox
            {
                Text = "Use main form dialog for Create",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Enabled = false,
                Margin = new Padding(0, 0, 0, 12)
            };

            chkMainFormEdit = new CheckBox
            {
                Text = "Use main form dialog for Edit",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Enabled = false,
                Margin = new Padding(0, 0, 0, 30)
            };

            btnSavePublish = new Button
            {
                Text = "Save & Publish",
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Height = 44,
                Width = 200,
                Visible = false
            };
            btnSavePublish.FlatAppearance.BorderSize = 0;
            btnSavePublish.Click += btnSavePublish_Click;

            content.Controls.Add(lblConfigMessage);
            content.Controls.Add(chkDisableMru);
            content.Controls.Add(chkDisableNew);
            content.Controls.Add(chkMainFormCreate);
            content.Controls.Add(chkMainFormEdit);
            content.Controls.Add(btnSavePublish);

            panelConfig.Controls.Add(content);
            panelConfig.Controls.Add(headerConfig); // Header on top

            mainLayout.Controls.Add(panelTables, 0, 0);
            mainLayout.Controls.Add(panelLookups, 1, 0);
            mainLayout.Controls.Add(panelConfig, 2, 0);

            this.Controls.Add(mainLayout);
            this.Controls.SetChildIndex(mainLayout, 0);
        }

        private void GridLookups_SelectionChanged(object sender, EventArgs e)
        {
            UpdateConfigPanel();
        }

        private DataGridView CreateStyledGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(52, 58, 70),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(70, 76, 90),
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ColumnHeadersHeight = 40,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(62, 68, 82),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    Padding = new Padding(12, 0, 0, 0)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(52, 58, 70),
                    ForeColor = Color.White,
                    SelectionBackColor = Color.FromArgb(0, 122, 204),
                    SelectionForeColor = Color.White,
                    Padding = new Padding(12, 0, 0, 0)
                },
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            grid.RowTemplate.Height = 36;
            return grid;
        }

        private Panel CreateModernPanel(string title, string subtitle)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0) };

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(0, 122, 204),
                Padding = new Padding(16, 12, 16, 12)
            };

            var lblTitle = new Label
            {
                Text = title,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 13F),
                AutoSize = true,
                Location = new Point(0, 0)
            };

            var lblSubtitle = new Label
            {
                Text = subtitle,
                ForeColor = Color.FromArgb(220, 240, 255),
                Font = new Font("Segoe UI", 9F),
                AutoSize = false,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 28, 0, 0)
            };

            header.Controls.Add(lblSubtitle);
            header.Controls.Add(lblTitle);
            lblTitle.BringToFront();

            panel.Controls.Add(header);

            return panel;
        }
    }
}