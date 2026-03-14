using Risk_Manager.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Risk_Manager
{
    /// <summary>
    /// Dialog for fetching trades from Quantower's GetTrades API and previewing them
    /// before importing into the trading journal. Allows the user to set per-trade
    /// Notes and FollowedPlan fields prior to import.
    /// </summary>
    public class GetTradesImportDialog : Form
    {
        // Callback set by the caller to fetch trades for a given date range
        public Func<DateTime, DateTime, GetTradesService.FetchResult> FetchTradesCallback { get; set; }

        // Trades selected by the user for import (populated when DialogResult == OK)
        public List<JournalTrade> SelectedTrades { get; private set; } = new List<JournalTrade>();

        // Internal state
        private List<JournalTrade> _fetchedTrades = new List<JournalTrade>();

        // Controls
        private DateTimePicker _fromPicker;
        private DateTimePicker _toPicker;
        private Button _fetchButton;
        private Label _statusLabel;
        private DataGridView _tradesGrid;
        private CheckBox _selectAllCheckbox;
        private Button _importButton;
        private Button _cancelButton;

        // Dark theme colours (mirror CsvImportPreviewDialog)
        private static readonly Color DarkBackground = Color.FromArgb(30, 30, 30);
        private static readonly Color CardBackground = Color.FromArgb(40, 40, 40);
        private static readonly Color TextWhite = Color.FromArgb(240, 240, 240);
        private static readonly Color AccentGreen = Color.FromArgb(50, 150, 50);
        private static readonly Color AccentBlue = Color.FromArgb(50, 100, 200);
        private static readonly Color AccentPurple = Color.FromArgb(100, 50, 200);

        public GetTradesImportDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Get Trades from Quantower";
            this.Size = new Size(1300, 750);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(900, 550);
            this.BackColor = DarkBackground;

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = DarkBackground
            };

            // ── Title ─────────────────────────────────────────────────────────────
            var titleLabel = new Label
            {
                Text = "📈 Get Trades from Quantower API",
                Dock = DockStyle.Top,
                Height = 44,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = TextWhite,
                TextAlign = ContentAlignment.MiddleLeft,
                UseCompatibleTextRendering = true
            };
            mainPanel.Controls.Add(titleLabel);

            // ── Date-range picker panel ───────────────────────────────────────────
            var dateRangePanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = CardBackground,
                Padding = new Padding(12, 8, 12, 8)
            };

            var fromLabel = new Label
            {
                Text = "From:",
                Location = new Point(12, 18),
                AutoSize = true,
                Font = new Font("Segoe UI", 10),
                ForeColor = TextWhite
            };
            dateRangePanel.Controls.Add(fromLabel);

            _fromPicker = new DateTimePicker
            {
                Location = new Point(60, 14),
                Width = 180,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "M/d/yyyy h:mm tt",
                Value = DateTime.Today,
                Font = new Font("Segoe UI", 10)
            };
            dateRangePanel.Controls.Add(_fromPicker);

            var toLabel = new Label
            {
                Text = "To:",
                Location = new Point(260, 18),
                AutoSize = true,
                Font = new Font("Segoe UI", 10),
                ForeColor = TextWhite
            };
            dateRangePanel.Controls.Add(toLabel);

            _toPicker = new DateTimePicker
            {
                Location = new Point(290, 14),
                Width = 180,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "M/d/yyyy h:mm tt",
                Value = DateTime.Now,
                Font = new Font("Segoe UI", 10)
            };
            dateRangePanel.Controls.Add(_toPicker);

            _fetchButton = new Button
            {
                Text = "🔄 Fetch Trades",
                Location = new Point(490, 12),
                Width = 140,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = AccentPurple,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                UseCompatibleTextRendering = true
            };
            _fetchButton.FlatAppearance.BorderSize = 0;
            _fetchButton.Click += FetchButton_Click;
            dateRangePanel.Controls.Add(_fetchButton);

            _statusLabel = new Label
            {
                Location = new Point(645, 18),
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(180, 180, 180)
            };
            dateRangePanel.Controls.Add(_statusLabel);

            mainPanel.Controls.Add(dateRangePanel);

            // ── Select-all checkbox ──────────────────────────────────────────────
            var checkboxPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = DarkBackground
            };

            _selectAllCheckbox = new CheckBox
            {
                Text = "Select All",
                Location = new Point(0, 6),
                Font = new Font("Segoe UI", 10),
                ForeColor = TextWhite,
                Checked = true,
                AutoSize = true
            };
            _selectAllCheckbox.CheckedChanged += SelectAllCheckbox_CheckedChanged;
            checkboxPanel.Controls.Add(_selectAllCheckbox);
            mainPanel.Controls.Add(checkboxPanel);

            // ── Trade grid ────────────────────────────────────────────────────────
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
            _tradesGrid.DefaultCellStyle.Padding = new Padding(4);

            _tradesGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 45);
            _tradesGrid.ColumnHeadersDefaultCellStyle.ForeColor = TextWhite;
            _tradesGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            SetupGridColumns();
            mainPanel.Controls.Add(_tradesGrid);

            // ── Bottom buttons panel ─────────────────────────────────────────────
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
                Margin = new Padding(0, 0, 10, 0),
                Enabled = false
            };
            _importButton.FlatAppearance.BorderSize = 0;
            _importButton.Click += ImportButton_Click;
            buttonsPanel.Controls.Add(_importButton);

            mainPanel.Controls.Add(buttonsPanel);

            this.Controls.Add(mainPanel);
        }

        /// <summary>
        /// Sets up the DataGridView columns, including editable Notes and FollowedPlan.
        /// </summary>
        private void SetupGridColumns()
        {
            _tradesGrid.Columns.Clear();

            // Import selection checkbox
            _tradesGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Import",
                HeaderText = "Import",
                Width = 60,
                TrueValue = true,
                FalseValue = false,
                ReadOnly = false
            });

            _tradesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Account", HeaderText = "Account", ReadOnly = true, Width = 130 });
            _tradesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Date", ReadOnly = true, Width = 90 });
            _tradesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Symbol", HeaderText = "Symbol", ReadOnly = true, Width = 80 });
            _tradesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Trade Type", ReadOnly = true, Width = 90 });
            _tradesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Outcome", HeaderText = "Outcome", ReadOnly = true, Width = 75 });
            _tradesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "EntryTime", HeaderText = "Entry Time", ReadOnly = true, Width = 110 });
            _tradesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExitTime", HeaderText = "Exit Time", ReadOnly = true, Width = 110 });
            _tradesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qty", HeaderText = "Contracts", ReadOnly = true, Width = 90 });
            _tradesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PL", HeaderText = "Gross P/L", ReadOnly = true, Width = 90 });
            _tradesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fees", HeaderText = "Fees", ReadOnly = true, Width = 70 });
            _tradesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "NetPL", HeaderText = "Net P/L", ReadOnly = true, Width = 90 });

            // Editable fields – users fill these in before importing
            _tradesGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "FollowedPlan",
                HeaderText = "Followed Plan",
                Width = 100,
                TrueValue = true,
                FalseValue = false,
                ReadOnly = false
            });

            _tradesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Notes",
                HeaderText = "Notes (editable)",
                ReadOnly = false,
                MinimumWidth = 150
            });

            _tradesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Status",
                HeaderText = "Status",
                ReadOnly = true,
                Width = 80
            });
        }

        // ── Event Handlers ────────────────────────────────────────────────────────

        private void FetchButton_Click(object sender, EventArgs e)
        {
            if (FetchTradesCallback == null)
            {
                MessageBox.Show("No fetch callback configured.", "Configuration Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DateTime from = _fromPicker.Value.Date;
            DateTime to = _toPicker.Value;

            if (from > to)
            {
                MessageBox.Show("'From' date must be before or equal to 'To' date.", "Invalid Date Range",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _fetchButton.Enabled = false;
            _statusLabel.Text = "Fetching trades…";
            Application.DoEvents();

            try
            {
                var result = FetchTradesCallback(from, to);

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    _statusLabel.Text = $"Error: {result.ErrorMessage}";
                    MessageBox.Show($"Error fetching trades:\n\n{result.ErrorMessage}",
                        "Fetch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _fetchedTrades = result.Trades ?? new List<JournalTrade>();
                PopulateGrid(_fetchedTrades);

                int newCount = _fetchedTrades.Count(t => !TradingJournalService.Instance.IsGlobalDuplicate(t));
                int dupCount = _fetchedTrades.Count - newCount;
                string fillsInfo = result.TotalFills > 0 ? $" ({result.TotalFills} fills)" : "";
                if (_fetchedTrades.Count > 0)
                {
                    string dupInfo = dupCount > 0 ? $", {dupCount} duplicate(s) skipped" : "";
                    _statusLabel.Text = $"Found {_fetchedTrades.Count} trade(s){fillsInfo}: {newCount} new{dupInfo}.";
                }
                else
                {
                    _statusLabel.Text = $"No trades found for the selected date range{fillsInfo}.";
                }

                _importButton.Enabled = newCount > 0;
            }
            finally
            {
                _fetchButton.Enabled = true;
            }
        }

        private void PopulateGrid(List<JournalTrade> trades)
        {
            _tradesGrid.Rows.Clear();

            var dimFore = Color.FromArgb(100, 100, 100);
            var dimBack = Color.FromArgb(30, 30, 30);
            var dupStatusColor = Color.FromArgb(200, 80, 80);
            var newStatusColor = Color.FromArgb(80, 180, 80);

            foreach (var trade in trades)
            {
                bool isDuplicate = TradingJournalService.Instance.IsGlobalDuplicate(trade);

                var row = _tradesGrid.Rows[_tradesGrid.Rows.Add()];
                row.Cells["Import"].Value = !isDuplicate;
                row.Cells["Import"].ReadOnly = isDuplicate;
                row.Cells["Account"].Value = trade.Account ?? "";
                row.Cells["Date"].Value = trade.Date.ToString("MM/dd/yyyy");
                row.Cells["Symbol"].Value = trade.Symbol;
                row.Cells["Type"].Value = trade.TradeType;
                row.Cells["Outcome"].Value = trade.Outcome;
                row.Cells["EntryTime"].Value = trade.EntryTime;
                row.Cells["ExitTime"].Value = trade.ExitTime;
                row.Cells["Qty"].Value = trade.Contracts;
                row.Cells["PL"].Value = trade.PL.ToString("C2");
                row.Cells["Fees"].Value = trade.Fees.ToString("C2");
                row.Cells["NetPL"].Value = trade.NetPL.ToString("C2");
                row.Cells["FollowedPlan"].Value = false;
                row.Cells["Notes"].Value = "";
                row.Cells["Status"].Value = isDuplicate ? "Duplicate" : "New";
                row.Tag = trade;

                if (isDuplicate)
                {
                    row.DefaultCellStyle.ForeColor = dimFore;
                    row.DefaultCellStyle.BackColor = dimBack;
                    row.Cells["Status"].Style.ForeColor = dupStatusColor;
                }
                else
                {
                    row.Cells["Status"].Style.ForeColor = newStatusColor;

                    // Colour-code outcome
                    if (trade.Outcome == "Win") row.Cells["Outcome"].Style.ForeColor = Color.LightGreen;
                    else if (trade.Outcome == "Loss") row.Cells["Outcome"].Style.ForeColor = Color.LightCoral;

                    // Colour-code Net P/L
                    row.Cells["NetPL"].Style.ForeColor = trade.NetPL > 0 ? Color.LightGreen : (trade.NetPL < 0 ? Color.LightCoral : TextWhite);
                    row.Cells["PL"].Style.ForeColor = trade.PL > 0 ? Color.LightGreen : (trade.PL < 0 ? Color.LightCoral : TextWhite);
                }
            }
        }

        private void SelectAllCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in _tradesGrid.Rows)
            {
                if (!row.Cells["Import"].ReadOnly)
                    row.Cells["Import"].Value = _selectAllCheckbox.Checked;
            }
        }

        private void ImportButton_Click(object sender, EventArgs e)
        {
            SelectedTrades.Clear();

            // Commit any pending edits in the grid before reading values
            _tradesGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            _tradesGrid.EndEdit();

            foreach (DataGridViewRow row in _tradesGrid.Rows)
            {
                if (row.Cells["Import"].Value is bool isChecked && isChecked)
                {
                    if (row.Tag is JournalTrade trade)
                    {
                        // Apply user-edited Notes and FollowedPlan back to the trade object
                        trade.Notes = row.Cells["Notes"].Value?.ToString() ?? "";
                        trade.FollowedPlan = row.Cells["FollowedPlan"].Value is bool fp ? fp : false;
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
