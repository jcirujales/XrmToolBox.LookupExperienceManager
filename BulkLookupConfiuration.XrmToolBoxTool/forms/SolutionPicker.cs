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
        public Solution SelectedSolution { get; private set; }

        private readonly ListBox listBoxSolutions;

        public SolutionPicker(List<Solution> solutions)
        {
            this.Text = "Select Solution";
            this.Size = new Size(560, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = this.MinimizeBox = false;
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
                Text = "Select a Solution",
                Font = new Font("Segoe UI Semibold", 16F),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(24, 24)
            };
            var lblSubtitle = new Label
            {
                Text = "Choose the solution to analyze lookup controls",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(220, 240, 255),
                AutoSize = true,
                Location = new Point(24, 56)
            };
            header.Controls.Add(lblTitle);
            header.Controls.Add(lblSubtitle);

            // ListBox – clean, single-select, beautiful
            listBoxSolutions = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(52, 58, 70),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                ItemHeight = 40,
                SelectionMode = SelectionMode.One,
                DrawMode = DrawMode.OwnerDrawFixed
            };

            // Custom drawing for modern look
            listBoxSolutions.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;

                var item = solutions[e.Index];
                var text = item.FriendlyName ?? item.UniqueName;

                e.DrawBackground();
                var brush = (e.State & DrawItemState.Selected) == DrawItemState.Selected
                    ? Brushes.White
                    : Brushes.LightGray;

                var bg = (e.State & DrawItemState.Selected) == DrawItemState.Selected
                    ? Color.FromArgb(0, 122, 204)
                    : Color.FromArgb(52, 58, 70);

                using (var bgBrush = new SolidBrush(bg))
                    e.Graphics.FillRectangle(bgBrush, e.Bounds);

                e.Graphics.DrawString(
                    text,
                    new Font("Segoe UI", 10F),
                    brush,
                    new PointF(e.Bounds.X + 16, e.Bounds.Y + 10)
                );

                e.DrawFocusRectangle();
            };

            listBoxSolutions.Items.AddRange(solutions.ToArray());

            // Double-click = OK
            listBoxSolutions.DoubleClick += (s, e) =>
            {
                if (listBoxSolutions.SelectedItem is Solution sol)
                {
                    SelectedSolution = sol;
                    DialogResult = DialogResult.OK;
                    Close();
                }
            };

            // Buttons
            var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 70, Padding = new Padding(20, 15, 20, 15) };

            var btnOK = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Enabled = false,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Size = new Size(100, 38),
                Location = new Point(340, 15)
            };
            btnOK.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 76, 90),
                ForeColor = Color.White,
                Size = new Size(100, 38),
                Location = new Point(450, 15)
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            // Enable OK button only when something is selected
            listBoxSolutions.SelectedIndexChanged += (s, e) =>
            {
                var hasSelection = listBoxSolutions.SelectedItem != null;
                btnOK.Enabled = hasSelection;
                if (hasSelection)
                    SelectedSolution = (Solution)listBoxSolutions.SelectedItem;
            };

            btnOK.Click += (s, e) =>
            {
                if (listBoxSolutions.SelectedItem is Solution sol)
                    SelectedSolution = sol;
            };

            btnPanel.Controls.Add(btnOK);
            btnPanel.Controls.Add(btnCancel);

            // Layout
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                Padding = new Padding(0)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));

            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(listBoxSolutions, 0, 1);
            layout.Controls.Add(btnPanel, 0, 2);

            this.Controls.Add(layout);
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }
    }
}