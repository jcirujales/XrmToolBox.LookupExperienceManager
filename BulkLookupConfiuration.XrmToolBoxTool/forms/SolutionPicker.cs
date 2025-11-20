using BulkLookupConfiguration.XrmToolBoxTool.model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace BulkLookupConfiguration.XrmToolBoxTool.forms
{
    public partial class SolutionPicker : Form
    {
        public List<Solution> SelectedSolutions { get; private set; } = new List<Solution>();

        public SolutionPicker(List<Solution> solutions)
        {
            InitializeComponent();
            checkedListBoxSolutions.Items.AddRange(solutions.ToArray()); 
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            SelectedSolutions = checkedListBoxSolutions.CheckedItems
                .Cast<Solution>()
                .ToList();

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
