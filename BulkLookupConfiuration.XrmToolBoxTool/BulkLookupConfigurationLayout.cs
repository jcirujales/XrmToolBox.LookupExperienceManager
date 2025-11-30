using BulkLookupConfiguration.XrmToolBoxTool.Model;
using System.Drawing;
using System.Windows.Forms;

namespace BulkLookupConfiguration.XrmToolBoxTool
{
    public static class BulkLookupConfigurationLayout
    {
        public static void SetupHeader(BulkLookupConfigurationControl mainControl)
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

            mainControl.lblSelectedSolution = new ToolStripLabel("No solution selected")
            {
                ForeColor = Color.FromArgb(180, 200, 255),
                Font = new Font("Segoe UI", 10F),
                Alignment = ToolStripItemAlignment.Right,
                Margin = new Padding(15, 0, 0, 0),
                AutoSize = true
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

            mainControl.toolbar.Items.Add(mainControl.btnSolutions);
            mainControl.toolbar.Items.Add(mainControl.lblSelectedSolution);

            mainControl.Controls.Add(mainControl.toolbar);
            mainControl.Controls.Add(mainControl.statusPanel);
        }

        public static void SetupModernLayout(BulkLookupConfigurationControl mainControl)
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
            mainControl.gridTables = CreateStyledGrid();
            mainControl.gridTables.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { HeaderText = "Display Name", FillWeight = 55, Name = "displayName", DataPropertyName="displayName" },
                new DataGridViewTextBoxColumn { HeaderText = "Schema Name", FillWeight = 45, Name = "schemaName", DataPropertyName="schemaName" }
            });
            panelTables.Controls.Add(mainControl.gridTables);
            panelTables.Controls.SetChildIndex(mainControl.gridTables, 0);

            // MIDDLE: Lookup Controls
            var panelLookups = CreateModernPanel("Lookup Controls", "Select lookup fields to configure");

            mainControl.gridLookups = CreateStyledGrid();
            mainControl.gridLookups.MultiSelect = true;
            mainControl.gridLookups.AutoGenerateColumns = false; // ← CRITICAL

            mainControl.gridLookups.Columns.AddRange(new DataGridViewColumn[]
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
                Padding = new Padding(24),
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                WrapContents = false
            };

            mainControl.lblConfigMessage = new Label
            {
                Text = "Selected: 0 lookup controls",
                ForeColor = Color.FromArgb(180, 180, 255),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 20)
            };

            mainControl.chkDisableNew = new CheckBox
            {
                Text = "Enable + New button",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Enabled = false,
                Margin = new Padding(0, 0, 0, 12)
            };

            mainControl.chkDisableMru = new CheckBox
            {
                Text = "Hide Recently Used (MRU)",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Enabled = false,
                Margin = new Padding(0, 0, 0, 12)
            };

            mainControl.chkMainFormCreate = new CheckBox
            {
                Text = "Use main form dialog for Create",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Enabled = false,
                Margin = new Padding(0, 0, 0, 12)
            };

            mainControl.chkMainFormEdit = new CheckBox
            {
                Text = "Use main form dialog for Edit",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Enabled = false,
                Margin = new Padding(0, 0, 0, 30)
            };

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
    }
}
