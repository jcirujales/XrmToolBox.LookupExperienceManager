using XrmToolBox.LookupExperienceManager.Actions;
using XrmToolBox.LookupExperienceManager.model;
using XrmToolBox.LookupExperienceManager.Properties;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using XrmToolBox.Extensibility;
using Label = System.Windows.Forms.Label;

namespace XrmToolBox.LookupExperienceManager
{
    public partial class LookupExperienceManagerControl : PluginControlBase
    {
        public bool isSystemUpdate = true;

        public Label lblConfigMessage;
        public Label lblTitle;
        public Panel statusPanel;
        public ToolStrip toolbar;
        public ToolStripButton btnSolutions;
        public ToolStripButton btnRefresh;
        public ToolStripLabel lblSelectedSolution;
        public Settings mySettings;
        public CheckBox chkDisableNew;
        public CheckBox chkDisableMru;
        public CheckBox chkMainFormCreate;
        public CheckBox chkMainFormEdit;
        public Button btnSavePublish;
        public TextBox searchBox;
        public TextBox selectedTable;
        public DataGridView gridTables;
        public DataGridView gridLookups;

        public Solution selectedSolution = null;
        public string selectedTableSchemaName = string.Empty;

        public LookupExperienceManagerControl()
        {
            this.PluginIcon = new Icon(new MemoryStream(Resources.heartlookup_32_nobg_icon));
            this.TabIcon = Resources.heartlookup_32_nobg;

            LookupExperienceManagerLayout.SetupHeader(this);
            LookupExperienceManagerLayout.SetupModernLayout(this);

            this.Load += LookupExperienceManagerControl_Load;
            this.OnCloseTool += LookupExperienceManagerControl_OnCloseTool;

            btnSolutions.Click += (s, e) => ExecuteMethod(LoadSolutions_Click);
            btnSavePublish.Click += (s, e) => ExecuteMethod(SaveAndPublish_Click);
            btnRefresh.Click += (s, e) => ExecuteMethod(Refresh_Click);
            gridLookups.SelectionChanged += (s, e) => ExecuteMethod(GridLookup_SelectionChanged);
            gridTables.SelectionChanged += (s, e) => ExecuteMethod(GridTables_SelectionChanged);
            
        }
        /// <summary>
        /// This event occurs when the plugin is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LookupExperienceManagerControl_OnCloseTool(object sender, EventArgs e)
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
            LookupExperienceManagerLayout.ClearGridsAndSelections(this);
            if (mySettings != null && detail != null)
            {
                mySettings.LastUsedOrganizationWebappUrl = detail.WebApplicationUrl;
                LogInfo("Connection updated to: {0}", detail.WebApplicationUrl);
            }
        }
        private void LookupExperienceManagerControl_Load(object sender, EventArgs e)
        {
            LoadSettings();

        }
        private void LoadSettings()
        {
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

        private void LoadSolutions_Click()
        {
            SolutionActions.LoadSolutions(this);
        }
        private void SaveAndPublish_Click()
        {
            SolutionActions.SaveAndPublishCustomizations(this, Service);
        }
        private void Refresh_Click()
        {
            SolutionActions.RefreshMetadata(this, Service);
        }
        private void GridLookup_SelectionChanged()
        {
            SolutionActions.UpdateConfigPanel(this);
        }
        private void GridTables_SelectionChanged()
        {
            SolutionActions.OnTargetEntitySelect(this, Service);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LookupExperienceManagerControl));
            this.SuspendLayout();
            this.Name = "LookupExperienceManagerControl";
            this.PluginIcon = ((System.Drawing.Icon)(resources.GetObject("$this.PluginIcon")));
            this.ResumeLayout(false);

        }
    }
}