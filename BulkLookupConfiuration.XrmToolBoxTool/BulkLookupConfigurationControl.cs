using BulkLookupConfiguration.XrmToolBoxTool.Actions;
using BulkLookupConfiguration.XrmToolBoxTool.Services;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Windows.Forms;
using XrmToolBox.Extensibility;
using Label = System.Windows.Forms.Label;

namespace BulkLookupConfiguration.XrmToolBoxTool
{
    public partial class BulkLookupConfigurationControl : PluginControlBase
    {
        public Label lblSelectedSolution;
        public Label lblConfigMessage;
        public Label lblTitle;
        public Panel statusPanel;
        public ToolStrip toolbar;
        public ToolStripButton btnClose;
        public ToolStripButton btnSolutions;
        public ToolStripButton btnSample;
        public Settings mySettings;
        public CheckBox chkDisableNew;
        public CheckBox chkDisableMru;
        public CheckBox chkMainFormCreate;
        public CheckBox chkMainFormEdit;
        public Button btnSavePublish;
        public DataGridView gridTables;
        public DataGridView gridLookups;

        public BulkLookupConfigurationControl()
        {
            BulkLookupConfigurationLayout.SetupHeader(this);
            BulkLookupConfigurationLayout.SetupModernLayout(this);

            this.Load += BulkLookupConfigurationControl_Load;
            this.OnCloseTool += BulkLookupConfigurationControl_OnCloseTool;

            btnClose.Click += (s, e) => CloseTool();
            btnSolutions.Click += (s, e) => SolutionActions.LoadSolutions(this);
            btnSavePublish.Click += (s, e) => SolutionActions.SaveAndPublishCustomizations(this, Service);
            gridLookups.SelectionChanged += (s, e) => SolutionActions.UpdateConfigPanel(this);
            gridTables.SelectionChanged += (s, e) => SolutionActions.OnTargetEntitySelect(this, Service);
            
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
    }
}