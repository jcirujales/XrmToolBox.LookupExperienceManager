using BulkLookupConfiguration.XrmToolBoxTool.model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BulkLookupConfiguration.XrmToolBoxTool.forms
{
    public partial class SolutionPicker : Form
    {
        public List<Solution> SelectedSolutions { get; private set; } = new List<Solution>();

        private readonly CheckedListBox checkedListBoxSolutions;

        public SolutionPicker(List<Solution> solutions)
        {
            // Modern form setup
            this.Text = "Select Solutions";
            this.Size = new Size(560, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(40, 44, 52);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5F);
            this.Padding = new Padding(1);

            // Header
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(0, 122, 204)
            };
            var lblTitle = new Label
            {
                Text = "Select Solution(s)",
                Font = new Font("Segoe UI Semibold", 16F),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(24, 24)
            };
            var lblSubtitle = new Label
            {
                Text = "Check one or more solutions to analyze lookup fields",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(220, 240, 255),
                AutoSize = true,
                Location = new Point(24, 56)
            };
            header.Controls.Add(lblTitle);
            header.Controls.Add(lblSubtitle);

            // CheckedListBox (modern dark style)
            checkedListBoxSolutions = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(52, 58, 70),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                CheckOnClick = true,
                FormattingEnabled = true,
                ItemHeight = 32,
                Margin = new Padding(20)
            };
            checkedListBoxSolutions.Items.AddRange(solutions.ToArray());

            // Buttons panel
            var btnPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                Padding = new Padding(20, 15, 20, 15)
            };
            var btnOK = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Size = new Size(100, 38),
                Location = new Point(340, 15)
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Click += (s, e) =>
            {
                SelectedSolutions = checkedListBoxSolutions.CheckedItems
                    .Cast<Solution>()
                    .ToList();
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 76, 90),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                Size = new Size(100, 38),
                Location = new Point(450, 15)
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            btnPanel.Controls.Add(btnOK);
            btnPanel.Controls.Add(btnCancel);

            // Main layout
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                Padding = new Padding(0)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));     // header
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));    // list
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));     // buttons

            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(checkedListBoxSolutions, 0, 1);
            layout.Controls.Add(btnPanel, 0, 2);

            this.Controls.Add(layout);
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }
    }
}