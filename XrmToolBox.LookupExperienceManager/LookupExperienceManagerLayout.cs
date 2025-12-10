using XrmToolBox.LookupExperienceManager.Model;
using XrmToolBox.LookupExperienceManager.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace XrmToolBox.LookupExperienceManager
{
    public static class LookupExperienceManagerLayout
    {
        public static void SetupHeader(LookupExperienceManagerControl mainControl)
        {
            mainControl.Dock = DockStyle.Fill;
            mainControl.Margin = new Padding(0);

            mainControl.statusPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(30, 34, 42),
                Padding = new Padding(12, 0, 12, 0)
            };

            mainControl.lblTitle = new Label
            {
                Text = "Lookup Experience Manager",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                AutoSize = true,
                Dock = DockStyle.Left,
                Padding = new Padding(0, 8, 0, 0)
            };

            mainControl.lblSelectedSolution = new ToolStripLabel(Resources.DefaultSolutionMessage)
             {
                 ForeColor = Color.FromArgb(180, 200, 255),
                 Font = new Font("Segoe UI", 10F),
                 Alignment = ToolStripItemAlignment.Right,
                 Margin = new Padding(15, 0, 0, 0),
                 AutoSize = true,
            };

            mainControl.statusPanel.Controls.Add(mainControl.lblTitle);

            mainControl.toolbar = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                BackColor = Color.FromArgb(40, 44, 52),
                ForeColor = Color.White,
                Renderer = new ToolStripProfessionalRenderer(new CustomColors())
            };

            mainControl.btnSolutions = new ToolStripButton("Select Solution")
            {
                Image = Properties.Resources.Solutions_32,
                ImageScaling = ToolStripItemImageScaling.None,
                ToolTipText = "Select a solution to analyze"
            };

            var toolStripSeparator = new ToolStripSeparator();

            mainControl.btnRefresh = new ToolStripButton("Refresh Metadata")
            {
                Image = Properties.Resources.Refresh_32,
                ImageScaling = ToolStripItemImageScaling.None,
                ToolTipText = "Refresh Metadata"
            };

            mainControl.toolbar.Items.Add(mainControl.btnSolutions);
            mainControl.toolbar.Items.Add(toolStripSeparator);
            mainControl.toolbar.Items.Add(mainControl.btnRefresh);
            mainControl.toolbar.Items.Add(mainControl.lblSelectedSolution);

            mainControl.Controls.Add(mainControl.toolbar);
            mainControl.Controls.Add(mainControl.statusPanel);
        }
        public static void ClearGridsAndSelections(LookupExperienceManagerControl mainControl)
        {
            mainControl.gridTables.ClearSelection();
            mainControl.gridTables.DataSource = null;
            mainControl.gridLookups.ClearSelection();
            mainControl.gridLookups.DataSource = null;
            mainControl.lblSelectedSolution.Text = Resources.DefaultSolutionMessage;
            mainControl.lblSelectedSolution.ForeColor = Color.FromArgb(180, 200, 255);
        }
        public static void SetupModernLayout(LookupExperienceManagerControl mainControl)
        {
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(0),
                BackColor = Color.FromArgb(40, 44, 52)
            };

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            // LEFT: Target Entity
            var panelTables = CreateModernPanel("Tables", "Select a lookup table to see all of its related lookup controls.");
            mainControl.gridTables = CreateStyledGrid();
            mainControl.gridTables.Focus();

            var displayNameColumn = new DataGridViewTextBoxColumn { HeaderText = "Display Name", FillWeight = 55, Name = "displayName", DataPropertyName = "displayName" };
            var gridTableSchemaNameColumn = new DataGridViewTextBoxColumn { HeaderText = "Schema Name", FillWeight = 45, Name = "schemaName", DataPropertyName = "schemaName" };

            mainControl.gridTables.Columns.AddRange(new DataGridViewColumn[]{ displayNameColumn, gridTableSchemaNameColumn });

            SetSearchBox(mainControl, mainControl.gridTables, panelTables, displayNameColumn.DataPropertyName, gridTableSchemaNameColumn.DataPropertyName);
            panelTables.Controls.Add(mainControl.gridTables);
            panelTables.Controls.SetChildIndex(mainControl.gridTables, 0);

            // MIDDLE: Lookup Controls
            var panelLookups = CreateModernPanel("Lookup Controls", "Select lookup fields to configure. All related Lookup controls display, some may not be included in the selected solution. Only UNMANAGED forms will be displayed.");

            mainControl.gridLookups = CreateStyledGrid();
            mainControl.gridLookups.MultiSelect = true;
            mainControl.gridLookups.AutoGenerateColumns = false;

            var sourceEntityColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = "Source Entity",
                Name = "SourceEntity",
                DataPropertyName = "SourceEntity",
                FillWeight = 25
            };
            var formColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = "Form",
                Name = "Form",
                DataPropertyName = "Form",
                FillWeight = 25
            };
            var formIdColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = "Form ID",
                Name = "FormId",
                DataPropertyName = "FormId",
                Visible = false,
                FillWeight = 25
            };
            var formXmlColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = "Form XML",
                Name = "FormXml",
                DataPropertyName = "FormXml",
                Visible = false,
                FillWeight = 25
            };
            var labelColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = "Control Form Label",
                Name = "Label",
                DataPropertyName = "Label",
                FillWeight = 25
            };
            var LookupSchemaNameColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = "Schema Name",
                Name = "SchemaName",
                DataPropertyName = "SchemaName",
                FillWeight = 25
            };
            var isManaged = new DataGridViewCheckBoxColumn
            {
                HeaderText = "Managed",
                Name = "ismanaged",
                DataPropertyName = "ismanaged",
                FillWeight = 20,
                ReadOnly = true,
                Visible = false, // debugging purposes
            };
            var isSourceEntityCustomizable = new DataGridViewCheckBoxColumn
            {
                HeaderText = "Source Entity Customizable",
                Name = "issourceentitycustomizable",
                DataPropertyName = "issourceentitycustomizable",
                FillWeight = 20,
                ReadOnly = true,
                Visible = false, // debugging purposes
            };
            var isCustomizable = new DataGridViewCheckBoxColumn
            {
                HeaderText = "Customizable",
                Name = "iscustomizable",
                DataPropertyName = "iscustomizable",
                FillWeight = 20,
                ReadOnly = true,
                Visible = false, // debugging purposes
            };
            var disableMRUColumn = new DataGridViewCheckBoxColumn
            {
                HeaderText = "Disable MRU",
                Name = FormXMLAttributes.DisableMru,
                DataPropertyName = FormXMLAttributes.DisableMru,
                FillWeight = 20,
                ReadOnly = true,
            };
            var isInlineEditableColumn = new DataGridViewCheckBoxColumn
            {
                HeaderText = "+ New",
                Name = FormXMLAttributes.IsInlineNewEnabled,
                DataPropertyName = FormXMLAttributes.IsInlineNewEnabled,
                FillWeight = 20,
                ReadOnly = true,
            };
            var isMainFormCreateEnabledColumn = new DataGridViewCheckBoxColumn
            {
                HeaderText = "Main Form (Create)",
                Name = FormXMLAttributes.UseMainFormDialogForCreate,
                DataPropertyName = FormXMLAttributes.UseMainFormDialogForCreate,
                FillWeight = 20
            };
            var isMainFormEditEnabledColumn = new DataGridViewCheckBoxColumn
            {
                HeaderText = "Main Form (Edit)",
                Name = FormXMLAttributes.UseMainFormDialogForEdit,
                DataPropertyName = FormXMLAttributes.UseMainFormDialogForEdit,
                FillWeight = 20
            };
            mainControl.gridLookups.Columns.AddRange(new DataGridViewColumn[]
            {
                sourceEntityColumn,
                formColumn,
                formIdColumn,
                formXmlColumn,
                labelColumn,
                LookupSchemaNameColumn,
                isManaged,
                isSourceEntityCustomizable,
                isCustomizable,
                disableMRUColumn,
                isInlineEditableColumn,
                isMainFormCreateEnabledColumn,
                isMainFormEditEnabledColumn,
            });

            SetSelectedTable(mainControl, mainControl.gridLookups, panelLookups);
            panelLookups.Controls.Add(mainControl.gridLookups);
            panelLookups.Controls.SetChildIndex(mainControl.gridLookups, 0);

            // RIGHT: Configuration
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
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                WrapContents = false
            };

            mainControl.lblConfigMessage = new Label
            {
                Text = Resources.DefaultLookupSelectionMessage,
                ForeColor = Color.FromArgb(180, 180, 255),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };

            mainControl.chkDisableNew = new CheckBox
            {
                Text = "Enable + New button",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Enabled = false,
                Margin = new Padding(10, 0, 0, 12)
            };
            mainControl.chkDisableNew.Tag = "When ENABLED: Shows the '+ New' button on lookup fields.\n" +
                                "When DISABLED: Hides the '+ New' button — prevents inline record creation.";

            mainControl.chkDisableMru = new CheckBox
            {
                Text = "Disable Recently Used (MRU)",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Enabled = false,
                Margin = new Padding(10, 0, 0, 12)
            };
            mainControl.chkDisableMru.Tag = "When ENABLED: Hides the 'Recently Used' items at the top of the lookup control.\n" +
                               "Be cautious that allowing recently used values can unexpectedly show when lookup filters are applied.";

            mainControl.chkMainFormCreate = new CheckBox
            {
                Text = "Use Main Form Dialog for Create",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Enabled = false,
                Margin = new Padding(10, 0, 0, 12),
            };
            mainControl.chkMainFormCreate.Tag = "When ENABLED: Clicking '+ New' opens the main form as a dialog (not as a quick create or inline main form).";

            mainControl.chkMainFormEdit = new CheckBox
            {
                Text = "Use Main Form Dialog for Edit",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Enabled = false,
                Margin = new Padding(10, 0, 0, 30)
            };
            mainControl.chkMainFormEdit.Tag = "When ENABLED: Clicking the lookup value text, a main form opens as a dialog (not as an inline main form).";

            var toolTip = new ToolTip
            {
                AutoPopDelay = 10000,
                InitialDelay = 500,
                ShowAlways = true,
                IsBalloon = true,
                ToolTipIcon = ToolTipIcon.Info,
                ToolTipTitle = "Lookup Control Setting"
            };

            // Attach to all checkboxes
            toolTip.SetToolTip(mainControl.chkDisableNew, mainControl.chkDisableNew.Tag.ToString());
            toolTip.SetToolTip(mainControl.chkDisableMru, mainControl.chkDisableMru.Tag.ToString());
            toolTip.SetToolTip(mainControl.chkMainFormCreate, mainControl.chkMainFormCreate.Tag.ToString());
            toolTip.SetToolTip(mainControl.chkMainFormEdit, mainControl.chkMainFormEdit.Tag.ToString());

            mainControl.btnSavePublish = new Button
            {
                Text = "Save and Publish",
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Height = 44,
                Width = 200,
                Visible = false
            };
            mainControl.btnSavePublish.FlatAppearance.BorderSize = 0;


            content.Controls.Add(mainControl.lblConfigMessage);
            content.Controls.Add(mainControl.chkDisableMru);
            content.Controls.Add(mainControl.chkDisableNew);
            content.Controls.Add(mainControl.chkMainFormCreate);
            content.Controls.Add(mainControl.chkMainFormEdit);
            content.Controls.Add(mainControl.btnSavePublish);

            panelConfig.Controls.Add(content);
            panelConfig.Controls.Add(headerConfig); // Header on top

            mainLayout.Controls.Add(panelTables, 0, 0);
            mainLayout.Controls.Add(panelLookups, 1, 0);
            mainLayout.Controls.Add(panelConfig, 2, 0);

            mainControl.Controls.Add(mainLayout);
            mainControl.Controls.SetChildIndex(mainLayout, 0);
        }
        private static DataGridView CreateStyledGrid()
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
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    Padding = new Padding(2, 0, 0, 0)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(52, 58, 70),
                    ForeColor = Color.White,
                    SelectionBackColor = Color.FromArgb(0, 122, 204),
                    SelectionForeColor = Color.White,
                    Padding = new Padding(2, 0, 0, 0)
                },
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            grid.RowTemplate.Height = 36;
            return grid;
        }

        private static Panel CreateModernPanel(string title, string subtitle)
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
        private static void SetSelectedTable(LookupExperienceManagerControl mainControl, DataGridView grid, Panel panel)
        {
            mainControl.selectedTable = new TextBox
            {
                Text = Resources.DefaultTableSelectionMessage,
                ReadOnly = true,
                Dock = DockStyle.Top,
                Height = 36,
                Margin = new Padding(12, 12, 12, 0),
                BackColor = Color.FromArgb(52, 58, 70),
                BorderStyle = BorderStyle.FixedSingle,
                ForeColor = Color.FromArgb(180, 180, 255),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Padding = new Padding(32, 0, 12, 0),
                TabStop = false
            };

            List<object> originalData = new List<object>();

            // Add search box on top
            panel.Controls.Add(mainControl.selectedTable);
            panel.Controls.SetChildIndex(mainControl.selectedTable, 0);
        }
        private static void SetSearchBox(LookupExperienceManagerControl mainControl, DataGridView grid, Panel panel, params string[] searchColumns)
        {
            mainControl.searchBox = new TextBox
            {
                Text = Resources.SearchPlaceholderText,
                Dock = DockStyle.Top,
                Height = 36,
                Margin = new Padding(12, 12, 12, 0),
                BackColor = Color.FromArgb(52, 58, 70),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10F),
                Padding = new Padding(32, 0, 12, 0),
            };

            // Placeholder color
            mainControl.searchBox.GotFocus += (s, e) =>
            {
                if (mainControl.searchBox.Text == Resources.SearchPlaceholderText) mainControl.searchBox.Text = "";
                mainControl.searchBox.ForeColor = Color.White;
            };
            mainControl.searchBox.LostFocus += (s, e) =>
            {
                mainControl.searchBox.ForeColor = Color.FromArgb(180, 180, 180);
            };

            List<object> originalData = new List<object>();

            mainControl.searchBox.TextChanged += (s, e) =>
            {
                if (mainControl.isSystemUpdate) return;

                mainControl.isSystemUpdate = true;
                var initialRecordSet = (List<object>)grid.Tag ?? new List<object>();
                try
                {
                    var term = mainControl.searchBox.Text.ToLower().Trim();
                    if (string.IsNullOrEmpty(term) || term == Resources.SearchPlaceholderText.ToLower())
                    {
                        grid.DataSource = initialRecordSet;
                    }
                    else
                    {
                        var filtered = initialRecordSet
                            .Where(item => searchColumns.Any(col =>
                                item.GetType()
                                    .GetProperty(col)?
                                    .GetValue(item)?
                                    .ToString()?
                                    .IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0))
                            .ToList();

                        grid.DataSource = filtered;
                    }
                    grid.ClearSelection();
                    mainControl.isSystemUpdate = false;
                }
                finally
                {
                    mainControl.isSystemUpdate = false;
                }
            };
            mainControl.searchBox.TabStop = false;

            // Add search box on top
            panel.Controls.Add(mainControl.searchBox);
            panel.Controls.SetChildIndex(mainControl.searchBox, 0);
        }

    }
}
