using BulkLookupConfiguration.XrmToolBoxTool.Actions;
using BulkLookupConfiguration.XrmToolBoxTool.forms;
using BulkLookupConfiguration.XrmToolBoxTool.model;
using BulkLookupConfiguration.XrmToolBoxTool.Services;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Drawing;
using System.Windows.Forms;
using XrmToolBox.Extensibility;
using Label = System.Windows.Forms.Label;

namespace BulkLookupConfiguration.XrmToolBoxTool
{
    public partial class BulkLookupConfigurationControl : PluginControlBase
    {
        private Settings mySettings;
        private Label lblSelectedSolution;
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

            // Toolbar
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
                // Image = Properties.Resources.Close_16x16, // TODO: Add Image
                ImageScaling = ToolStripItemImageScaling.SizeToFit,
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
                // Image = Properties.Resources.Rocket_16x16, // TODO: Add Image
                ToolTipText = "Run sample account query"
            };
            btnSample.Click += tsbSample_Click;

            toolbar.Items.Add(btnSolutions);
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(btnSample);
            toolbar.Items.Add(new ToolStripSeparator { Margin = new Padding(20, 0, 20, 0) });
            toolbar.Items.Add(btnClose);

            this.Controls.Add(toolbar);
            this.Controls.Add(statusPanel);  // Add status bar
        }

        private void OnSolutionSelected(Solution selectedSolution)
        {
            var solutionName = selectedSolution.FriendlyName ?? selectedSolution.UniqueName;
            var version = selectedSolution.Version ?? "?.?.?";

            // Update status label instead of MessageBox
            this.Invoke((MethodInvoker)(() =>
            {
                lblSelectedSolution.Text = $"Selected: {solutionName} v{version}";
                lblSelectedSolution.ForeColor = Color.FromArgb(100, 255, 150); // Success green
            }));

            SolutionActions.LoadEntitiesFromSolution(this, selectedSolution, entities =>
            {
                GridTables.Invoke((MethodInvoker)(() =>
                {
                    GridTables.Rows.Clear();
                    foreach (var entity in entities)
                    {
                        var name = entity.DisplayName?.UserLocalizedLabel?.Label ?? entity.LogicalName;
                        GridTables.Rows.Add(name, entity.LogicalName);
                    }
                }));
            });
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
                        picker.Dispose();   // TODO: Research
                    }
                }));
            });
        }

        // ===================================================================
        // FINAL MODERN UI — No overlap, no hacks, perfect
        // ===================================================================
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

            for (int i = 0; i < 3; i++)
                mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            // LEFT: Tables
            var panelTables = CreateModernPanel("Tables", "Select one or more tables to analyze");
            GridTables = CreateStyledGrid();
            GridTables.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { HeaderText = "Display Name", DataPropertyName = "DisplayName", FillWeight = 55 },
                new DataGridViewTextBoxColumn { HeaderText = "Schema Name", DataPropertyName = "LogicalName", FillWeight = 45 }
            });
            panelTables.Controls.Add(GridTables);
            panelTables.Controls.SetChildIndex(GridTables, 0);

            // MIDDLE: Lookup Controls
            var panelLookups = CreateModernPanel("Lookup Controls", "Select lookup fields to configure");
            GridLookups = CreateStyledGrid();
            GridLookups.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { HeaderText = "Target Entity",  FillWeight = 25 },
                new DataGridViewTextBoxColumn { HeaderText = "Form",           FillWeight = 25 },
                new DataGridViewTextBoxColumn { HeaderText = "Control Name",   FillWeight = 25 },
                new DataGridViewTextBoxColumn { HeaderText = "Schema Name",    FillWeight = 25 },
            });
            panelLookups.Controls.Add(GridLookups);
            panelLookups.Controls.SetChildIndex(GridLookups, 0);

            // RIGHT: Configuration
            var panelConfig = CreateModernPanel("Configuration", "Settings will appear when lookup(s) selected");
            var lbl = new Label
            {
                Text = "Select one or more lookup controls\nto configure their behavior",
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Segoe UI", 11F),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelConfig.Controls.Add(lbl);
            panelConfig.Controls.SetChildIndex(lbl, 0);

            mainLayout.Controls.Add(panelTables, 0, 0);
            mainLayout.Controls.Add(panelLookups, 1, 0);
            mainLayout.Controls.Add(panelConfig, 2, 0);

            this.Controls.Add(mainLayout);
            this.Controls.SetChildIndex(mainLayout, 0);
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
                MultiSelect = true,
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
            // This line must be OUTSIDE the initializer
            grid.RowTemplate.Height = 36;

            return grid;
        }

        private Panel CreateModernPanel(string title, string subtitle)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0) };

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.FromArgb(0, 122, 204)
            };

            var lblTitle = new Label
            {
                Text = title,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 13F),
                Location = new Point(16, 14),
                AutoSize = true
            };

            var lblSubtitle = new Label
            {
                Text = subtitle,
                ForeColor = Color.FromArgb(220, 240, 255),
                Font = new Font("Segoe UI", 9F),
                Location = new Point(16, 38),
                AutoSize = true
            };

            header.Controls.Add(lblTitle);
            header.Controls.Add(lblSubtitle);
            panel.Controls.Add(header);

            return panel;
        }
    }
}