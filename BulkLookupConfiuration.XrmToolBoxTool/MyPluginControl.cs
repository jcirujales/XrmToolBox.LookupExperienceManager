using BulkLookupConfiguration.XrmToolBoxTool.forms;
using BulkLookupConfiguration.XrmToolBoxTool.model;
using McTools.Xrm.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using XrmToolBox.Extensibility;

namespace BulkLookupConfiguration.XrmToolBoxTool
{
    public partial class MyPluginControl : PluginControlBase
    {
        private Settings mySettings;

        public MyPluginControl()
        {
            InitializeComponent();
        }

        private void MyPluginControl_Load(object sender, EventArgs e)
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

            // TODO: Your real work starts here — load lookup fields, etc.
        }

        private void tsbClose_Click(object sender, EventArgs e)
        {
            CloseTool();
        }

        private void tsbSample_Click(object sender, EventArgs e)
        {
            // The ExecuteMethod method handles connecting to an
            // organization if XrmToolBox is not yet connected
            ExecuteMethod(GetAccounts);
        }

        private void GetAccounts()
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Getting accounts",
                Work = (worker, args) =>
                {
                    args.Result = Service.RetrieveMultiple(new QueryExpression("account")
                    {
                        TopCount = 50
                    });
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    var result = args.Result as EntityCollection;
                    if (result != null)
                    {
                        MessageBox.Show($"Found {result.Entities.Count} accounts");
                    }
                }
            });
        }

        /// <summary>
        /// This event occurs when the plugin is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyPluginControl_OnCloseTool(object sender, EventArgs e)
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
            try
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
                            new ConditionExpression("uniquename", ConditionOperator.NotEqual, "Default")
                        }
                            },
                            Orders = { new OrderExpression("friendlyname", OrderType.Ascending) }
                        };

                        args.Result = Service.RetrieveMultiple(query);
                    },
                    PostWorkCallBack = (args) =>
                    {
                        if (args.Error != null)
                        {
                            MessageBox.Show(args.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                       
                        var solutions = ((EntityCollection)args.Result).Entities
                           .Select(record => record.ToEntity<Solution>())
                           .OrderBy(s => s.FriendlyName)
                           .ToList();

                        this.Invoke((MethodInvoker)(() =>
                        {
                            using (var picker = new SolutionPicker(solutions))
                            {
                                if (picker.ShowDialog(this) == DialogResult.OK)
                                    OnSolutionsSelected(picker.SelectedSolutions);
                            }
                        }));
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}