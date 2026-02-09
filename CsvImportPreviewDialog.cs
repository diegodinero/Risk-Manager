using Risk_Manager.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Risk_Manager
{
    /// <summary>
    /// Dialog for previewing and selecting trades to import from CSV
    /// </summary>
    public class CsvImportPreviewDialog : Form
    {
        private readonly List<JournalTrade> _allTrades;
        private readonly List<string> _errors;
        private readonly List<string> _warnings;
        private DataGridView _tradesGrid;
        private Label _summaryLabel;
        private Label _errorsLabel;
        private Button _importButton;
        private Button _cancelButton;
        private CheckBox _selectAllCheckbox;

        // Color scheme matching the dark theme
        private static readonly Color DarkBackground = Color.FromArgb(30, 30, 30);
        private static readonly Color CardBackground = Color.FromArgb(40, 40, 40);
        private static readonly Color TextWhite = Color.FromArgb(240, 240, 240);
        private static readonly Color AccentGreen = Color.FromArgb(50, 150, 50);
        private static readonly Color AccentRed = Color.FromArgb(200, 50, 50);
        private static readonly Color AccentBlue = Color.FromArgb(50, 100, 200);

        public List<JournalTrade> SelectedTrades { get; private set; }

        public CsvImportPreviewDialog(List<JournalTrade> trades, List<string> errors, List<string> warnings)
        {
            _allTrades = trades ?? new List<JournalTrade>();
            _errors = errors ?? new List<string>();
            _warnings = warnings ?? new List<string>();
            SelectedTrades = new List<JournalTrade>();

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "CSV Import Preview";
            this.Size = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(800, 500);
            this.BackColor = DarkBackground;

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = DarkBackground
            };

            // Title
            var titleLabel = new Label
            {
                Text = "📊 Import Trades from CSV",
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = TextWhite,
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(titleLabel);

            // Summary panel
            var summaryPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = CardBackground,
                Padding = new Padding(15),
                Margin = new Padding(0, 10, 0, 10)
            };

            _summaryLabel = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                ForeColor = TextWhite,
                AutoSize = false
            };
            summaryPanel.Controls.Add(_summaryLabel);
            mainPanel.Controls.Add(summaryPanel);

            // Errors/Warnings panel (if any)
            if (_errors.Count > 0 || _warnings.Count > 0)
            {
                var errorsPanel = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 100,
                    BackColor = Color.FromArgb(60, 30, 30),
                    Padding = new Padding(15),
                    Margin = new Padding(0, 0, 0, 10),
                    AutoScroll = true
                };

                _errorsLabel = new Label
                {
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Color.FromArgb(255, 150, 150),
                    AutoSize = false
                };
                errorsPanel.Controls.Add(_errorsLabel);
                mainPanel.Controls.Add(errorsPanel);
            }

            // Select all checkbox
            var checkboxPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = DarkBackground
            };

            _selectAllCheckbox = new CheckBox
            {
                Text = "Select All",
                Location = new Point(0, 5),
                Font = new Font("Segoe UI", 10),
                ForeColor = TextWhite,
                Checked = true,
                AutoSize = true
            };
            _selectAllCheckbox.CheckedChanged += SelectAllCheckbox_CheckedChanged;
            checkboxPanel.Controls.Add(_selectAllCheckbox);
            mainPanel.Controls.Add(checkboxPanel);

            // DataGridView for trades
            _tradesGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(35, 35, 35),
                GridColor = Color.FromArgb(60, 60, 60),
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false
            };

            _tradesGrid.DefaultCellStyle.BackColor = Color.FromArgb(35, 35, 35);
            _tradesGrid.DefaultCellStyle.ForeColor = TextWhite;
            _tradesGrid.DefaultCellStyle.SelectionBackColor = AccentBlue;
            _tradesGrid.DefaultCellStyle.SelectionForeColor = TextWhite;
            _tradesGrid.DefaultCellStyle.Padding = new Padding(5);

            _tradesGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 45);
            _tradesGrid.ColumnHeadersDefaultCellStyle.ForeColor = TextWhite;
            _tradesGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            mainPanel.Controls.Add(_tradesGrid);

            // Buttons panel
            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = DarkBackground,
                Padding = new Padding(0, 10, 0, 0)
            };

            _cancelButton = new Button
            {
                Text = "Cancel",
                Width = 120,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = TextWhite,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            _cancelButton.FlatAppearance.BorderSize = 0;
            _cancelButton.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            buttonsPanel.Controls.Add(_cancelButton);

            _importButton = new Button
            {
                Text = "Import Selected",
                Width = 150,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = AccentGreen,
                ForeColor = TextWhite,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Margin = new Padding(0, 0, 10, 0)
            };
            _importButton.FlatAppearance.BorderSize = 0;
            _importButton.Click += ImportButton_Click;
            buttonsPanel.Controls.Add(_importButton);

            mainPanel.Controls.Add(buttonsPanel);

            this.Controls.Add(mainPanel);
        }

        private void LoadData()
        {
            // Update summary
            UpdateSummary();

            // Update errors/warnings
            if (_errorsLabel != null)
            {
                var messages = new List<string>();
                if (_errors.Count > 0)
                {
                    messages.Add($"Errors ({_errors.Count}):");
                    messages.AddRange(_errors.Take(3).Select(e => $"  • {e}"));
                    if (_errors.Count > 3)
                        messages.Add($"  ... and {_errors.Count - 3} more");
                }
                if (_warnings.Count > 0)
                {
                    if (messages.Count > 0) messages.Add("");
                    messages.Add($"Warnings ({_warnings.Count}):");
                    messages.AddRange(_warnings.Take(3).Select(w => $"  • {w}"));
                    if (_warnings.Count > 3)
                        messages.Add($"  ... and {_warnings.Count - 3} more");
                }
                _errorsLabel.Text = string.Join(Environment.NewLine, messages);
            }

            // Setup grid columns
            _tradesGrid.Columns.Clear();

            // Checkbox column for selection
            var checkboxColumn = new DataGridViewCheckBoxColumn
            {
                Name = "Import",
                HeaderText = "Import",
                Width = 60,
                TrueValue = true,
                FalseValue = false,
                ReadOnly = false
            };
            _tradesGrid.Columns.Add(checkboxColumn);

            _tradesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Date", ReadOnly = true });
            _tradesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Symbol", HeaderText = "Symbol", ReadOnly = true });
            _tradesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Type", ReadOnly = true });
            _tradesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Outcome", HeaderText = "Outcome", ReadOnly = true });
            _tradesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "EntryTime", HeaderText = "Entry", ReadOnly = true });
            _tradesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExitTime", HeaderText = "Exit", ReadOnly = true });
            _tradesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Contracts", HeaderText = "Qty", ReadOnly = true });
            _tradesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PL", HeaderText = "P/L", ReadOnly = true });
            _tradesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fees", HeaderText = "Fees", ReadOnly = true });
            _tradesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "NetPL", HeaderText = "Net P/L", ReadOnly = true });

            // Load trades
            foreach (var trade in _allTrades)
            {
                var row = _tradesGrid.Rows[_tradesGrid.Rows.Add()];
                row.Cells["Import"].Value = true; // Selected by default
                row.Cells["Date"].Value = trade.Date.ToString("MM/dd/yyyy");
                row.Cells["Symbol"].Value = trade.Symbol;
                row.Cells["Type"].Value = trade.TradeType;
                row.Cells["Outcome"].Value = trade.Outcome;
                row.Cells["EntryTime"].Value = trade.EntryTime;
                row.Cells["ExitTime"].Value = trade.ExitTime;
                row.Cells["Contracts"].Value = trade.Contracts;
                row.Cells["PL"].Value = trade.PL.ToString("C2");
                row.Cells["Fees"].Value = trade.Fees.ToString("C2");
                row.Cells["NetPL"].Value = trade.NetPL.ToString("C2");
                row.Tag = trade; // Store trade object

                // Color code outcome
                if (trade.Outcome == "Win")
                {
                    row.Cells["Outcome"].Style.ForeColor = Color.LightGreen;
                }
                else if (trade.Outcome == "Loss")
                {
                    row.Cells["Outcome"].Style.ForeColor = Color.LightCoral;
                }

                // Color code P/L
                if (trade.NetPL > 0)
                {
                    row.Cells["NetPL"].Style.ForeColor = Color.LightGreen;
                }
                else if (trade.NetPL < 0)
                {
                    row.Cells["NetPL"].Style.ForeColor = Color.LightCoral;
                }
            }
        }

        private void UpdateSummary()
        {
            if (_allTrades.Count == 0)
            {
                _summaryLabel.Text = "No trades found in CSV file.";
                _importButton.Enabled = false;
                return;
            }

            var wins = _allTrades.Count(t => t.Outcome == "Win");
            var losses = _allTrades.Count(t => t.Outcome == "Loss");
            var breakevens = _allTrades.Count(t => t.Outcome == "Breakeven");
            var totalPL = _allTrades.Sum(t => t.NetPL);
            var winRate = _allTrades.Count > 0 ? (double)wins / _allTrades.Count * 100 : 0;

            _summaryLabel.Text = $"Total Trades: {_allTrades.Count}  |  " +
                                $"Wins: {wins}  |  Losses: {losses}  |  Breakeven: {breakevens}  |  " +
                                $"Win Rate: {winRate:F1}%  |  Total P/L: {totalPL:C2}";
        }

        private void SelectAllCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in _tradesGrid.Rows)
            {
                row.Cells["Import"].Value = _selectAllCheckbox.Checked;
            }
        }

        private void ImportButton_Click(object sender, EventArgs e)
        {
            SelectedTrades.Clear();

            foreach (DataGridViewRow row in _tradesGrid.Rows)
            {
                if (row.Cells["Import"].Value is bool isChecked && isChecked)
                {
                    if (row.Tag is JournalTrade trade)
                    {
                        SelectedTrades.Add(trade);
                    }
                }
            }

            if (SelectedTrades.Count == 0)
            {
                MessageBox.Show("Please select at least one trade to import.", "No Trades Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
