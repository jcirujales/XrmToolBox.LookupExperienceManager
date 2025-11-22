using BulkLookupConfiguration.XrmToolBoxTool.forms;
using BulkLookupConfiguration.XrmToolBoxTool.model;
using McTools.Xrm.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using XrmToolBox.Extensibility;
using Label = System.Windows.Forms.Label;

namespace BulkLookupConfiguration.XrmToolBoxTool
{
    public partial class BulkLookupConfigurationControl : PluginControlBase
    {
        private Settings mySettings;

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
                // Image = Properties.Resources.Close_16x16,
                ImageScaling = ToolStripItemImageScaling.SizeToFit,
                Alignment = ToolStripItemAlignment.Right
            };
            btnClose.Click += (s, e) => CloseTool();

            var btnSolutions = new ToolStripButton("Select Solutions")
            {
                Image = Properties.Resources.solutions_32,
                ImageScaling = ToolStripItemImageScaling.None,
                ToolTipText = "Select solutions to scan"
            };
            btnSolutions.Click += tsb_opensolutions_Click;

            var btnSample = new ToolStripButton("Sample Query")
            {
                // Image = Properties.Resources.Rocket_16x16,
                ToolTipText = "Run sample account query"
            };
            btnSample.Click += tsbSample_Click;

            toolbar.Items.Add(btnSolutions);
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(btnSample);
            toolbar.Items.Add(new ToolStripSeparator { Margin = new Padding(20, 0, 20, 0) });
            toolbar.Items.Add(btnClose);

            this.Controls.Add(toolbar);
        }

        // Custom colors for modern dark toolbar
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
            ExecuteMethod(WhoAmI);
        }

        private void LoadSettings()
        {
            ShowInfoNotification("This is a notification that can lead to XrmToolBox repository", new Uri("https://github.com/MscrmTools/XrmToolBox"));

            // Loads or creates the settings for the plugin
            if (!SettingsManager.Instance.TryLoad(GetType(), out mySettings))
            {
                mySettings = new Settings();
                LogWarning("Settings not found => a new settings file has been created!");
            }
            else
            {
                LogInfo("Settings found and loaded");
            }
        }

        private void WhoAmI()
        {
            Service.Execute(new WhoAmIRequest());
        }

        private void OnSolutionsSelected(List<Solution> selectedSolutions)
        {
            var names = string.Join(", ", selectedSolutions.Select(s => s.FriendlyName));
            MessageBox.Show($"You selected {selectedSolutions.Count} solution(s):\n\n{names}",
                "Solutions Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // TODO: Load entities from selected solutions into GridTables
        }

        private void tsbClose_Click(object sender, EventArgs e) => CloseTool();

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
                LogInfo("Connection has changed to: {0}", detail.WebApplicationUrl);
            }
        }

        private void tsb_opensolutions_Click(object sender, EventArgs e)
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading solutions...",
                Work = (worker, args) =>
                {
                    var query = new QueryExpression("solution")
                    {
                        ColumnSet = new ColumnSet("friendlyname", "uniquename", "ismanaged", "version"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("isvisible", ConditionOperator.Equal, true),
                            }
                        },
                        Orders = { new OrderExpression("friendlyname", OrderType.Ascending) }
                    };
                    args.Result = Service.RetrieveMultiple(query);
                },
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var solutions = ((EntityCollection)args.Result).Entities
                    .Select(entity =>
                    {
                        var sol = new Solution();
                        sol["uniquename"] = entity.GetAttributeValue<string>("uniquename");
                        sol["friendlyname"] = entity.GetAttributeValue<string>("friendlyname")
                                              ?? entity.GetAttributeValue<string>("uniquename");
                        sol["version"] = entity.GetAttributeValue<string>("version");
                        sol["ismanaged"] = entity.GetAttributeValue<bool>("ismanaged");
                        return sol;
                    })
                    .OrderBy(s => s.FriendlyName)
                    .ToList();

                    this.BeginInvoke((MethodInvoker)(() =>
                    {
                        using (var picker = new SolutionPicker(solutions))
                        {
                            if (picker.ShowDialog(this) == DialogResult.OK && picker.SelectedSolutions.Any())
                            {
                                OnSolutionsSelected(picker.SelectedSolutions);
                            }
                        }
                    }));
                }
            });
        }

        // ===================================================================
        // MODERN UI SETUP — This is the beautiful part
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

            // Left: Tables
            var panelTables = CreateModernPanel("Tables", "Select one or more tables to analyze");
            GridTables = CreateStyledGrid();
            GridTables.Columns.AddRange(new[]
            {
                new DataGridViewTextBoxColumn { HeaderText = "Display Name",  DataPropertyName = "DisplayName", FillWeight = 55 },
                new DataGridViewTextBoxColumn { HeaderText = "Schema Name",   DataPropertyName = "LogicalName", FillWeight = 45 }
            });
            panelTables.Controls.Add(GridTables);

            // Middle: Lookup Controls
            var panelLookups = CreateModernPanel("Lookup Controls", "Select lookup fields to configure");
            GridLookups = CreateStyledGrid();
            GridLookups.Columns.AddRange(new[]
            {
                new DataGridViewTextBoxColumn { HeaderText = "Control Name",   FillWeight = 30 },
                new DataGridViewTextBoxColumn { HeaderText = "Schema Name",    FillWeight = 30 },
                new DataGridViewTextBoxColumn { HeaderText = "Form",           FillWeight = 25 },
                new DataGridViewTextBoxColumn { HeaderText = "Target Entity",  FillWeight = 15 }
            });
            panelLookups.Controls.Add(GridLookups);

            // Right: Configuration (blank for now)
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
                }
            };

            // This line must be OUTSIDE the initializer
            grid.RowTemplate.Height = 36;

            return grid;
        }

        private Panel CreateModernPanel(string title, string subtitle)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(1) };

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