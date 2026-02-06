using Risk_Manager.Data;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Risk_Manager
{
    /// <summary>
    /// Dialog for adding or editing a trade entry in the trading journal
    /// </summary>
    public class TradeEntryDialog : Form
    {
        private JournalTrade _trade;
        private readonly string _accountNumber;
        private readonly bool _isEditMode;

        // Form controls
        private DateTimePicker datePicker;
        private TextBox symbolInput;
        private ComboBox outcomeCombo;
        private ComboBox typeCombo;
        private ComboBox modelInput;  // Changed from TextBox to ComboBox
        private TextBox sessionInput;
        private TextBox plInput;
        private TextBox rrInput;
        private TextBox entryTimeInput;
        private TextBox exitTimeInput;
        private TextBox entryPriceInput;
        private TextBox exitPriceInput;
        private TextBox contractsInput;
        private TextBox feesInput;
        private CheckBox followedPlanCheckbox;
        private ComboBox emotionsCombo;
        private TextBox notesInput;
        private Button saveButton;
        private Button cancelButton;

        public TradeEntryDialog(JournalTrade trade, string accountNumber)
        {
            _trade = trade ?? new JournalTrade();
            _accountNumber = accountNumber;
            _isEditMode = trade != null;

            InitializeComponent();
            LoadTradeData();
        }

        private void InitializeComponent()
        {
            this.Text = _isEditMode ? "Edit Trade" : "Add New Trade";
            this.Size = new Size(600, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(30, 30, 30);

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                AutoScroll = true,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            int yPos = 10;
            int labelWidth = 120;
            int inputWidth = 200;
            int spacing = 45;

            // Date
            AddLabel(mainPanel, "Date:", 10, yPos, labelWidth);
            datePicker = new DateTimePicker
            {
                Location = new Point(labelWidth + 20, yPos),
                Width = inputWidth,
                Format = DateTimePickerFormat.Short
            };
            mainPanel.Controls.Add(datePicker);
            yPos += spacing;

            // Symbol
            AddLabel(mainPanel, "Symbol:", 10, yPos, labelWidth);
            symbolInput = AddTextBox(mainPanel, labelWidth + 20, yPos, inputWidth);
            yPos += spacing;

            // Outcome
            AddLabel(mainPanel, "Outcome:", 10, yPos, labelWidth);
            outcomeCombo = new ComboBox
            {
                Location = new Point(labelWidth + 20, yPos),
                Width = inputWidth,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            outcomeCombo.Items.AddRange(new[] { "Win", "Loss", "Breakeven" });
            mainPanel.Controls.Add(outcomeCombo);
            yPos += spacing;

            // Trade Type
            AddLabel(mainPanel, "Type:", 10, yPos, labelWidth);
            typeCombo = new ComboBox
            {
                Location = new Point(labelWidth + 20, yPos),
                Width = inputWidth,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            typeCombo.Items.AddRange(new[] { "Long", "Short" });
            mainPanel.Controls.Add(typeCombo);
            yPos += spacing;

            // Model - Changed to ComboBox with models from TradingJournalService
            AddLabel(mainPanel, "Model/Strategy:", 10, yPos, labelWidth);
            modelInput = new ComboBox
            {
                Location = new Point(labelWidth + 20, yPos),
                Width = inputWidth,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            
            // Load existing models for this account
            if (!string.IsNullOrEmpty(_accountNumber))
            {
                var models = TradingJournalService.Instance.GetModels(_accountNumber);
                foreach (var model in models)
                {
                    if (!string.IsNullOrEmpty(model.Name))
                    {
                        modelInput.Items.Add(model.Name);
                    }
                }
            }
            
            mainPanel.Controls.Add(modelInput);
            yPos += spacing;

            // Session
            AddLabel(mainPanel, "Session:", 10, yPos, labelWidth);
            sessionInput = AddTextBox(mainPanel, labelWidth + 20, yPos, inputWidth);
            yPos += spacing;

            // P/L
            AddLabel(mainPanel, "P/L ($):", 10, yPos, labelWidth);
            plInput = AddTextBox(mainPanel, labelWidth + 20, yPos, inputWidth);
            yPos += spacing;

            // R:R
            AddLabel(mainPanel, "Risk:Reward:", 10, yPos, labelWidth);
            rrInput = AddTextBox(mainPanel, labelWidth + 20, yPos, inputWidth);
            yPos += spacing;

            // Entry Time
            AddLabel(mainPanel, "Entry Time:", 10, yPos, labelWidth);
            entryTimeInput = AddTextBox(mainPanel, labelWidth + 20, yPos, inputWidth);
            yPos += spacing;

            // Exit Time
            AddLabel(mainPanel, "Exit Time:", 10, yPos, labelWidth);
            exitTimeInput = AddTextBox(mainPanel, labelWidth + 20, yPos, inputWidth);
            yPos += spacing;

            // Entry Price
            AddLabel(mainPanel, "Entry Price:", 10, yPos, labelWidth);
            entryPriceInput = AddTextBox(mainPanel, labelWidth + 20, yPos, inputWidth);
            yPos += spacing;

            // Exit Price
            AddLabel(mainPanel, "Exit Price:", 10, yPos, labelWidth);
            exitPriceInput = AddTextBox(mainPanel, labelWidth + 20, yPos, inputWidth);
            yPos += spacing;

            // Contracts
            AddLabel(mainPanel, "Contracts:", 10, yPos, labelWidth);
            contractsInput = AddTextBox(mainPanel, labelWidth + 20, yPos, inputWidth);
            yPos += spacing;

            // Fees
            AddLabel(mainPanel, "Fees ($):", 10, yPos, labelWidth);
            feesInput = AddTextBox(mainPanel, labelWidth + 20, yPos, inputWidth);
            yPos += spacing;

            // Followed Plan
            followedPlanCheckbox = new CheckBox
            {
                Text = "Followed Trading Plan",
                Location = new Point(10, yPos),
                Width = labelWidth + inputWidth + 20,
                ForeColor = Color.White,
                Checked = true
            };
            mainPanel.Controls.Add(followedPlanCheckbox);
            yPos += spacing;

            // Emotions
            AddLabel(mainPanel, "Emotions:", 10, yPos, labelWidth);
            emotionsCombo = new ComboBox
            {
                Location = new Point(labelWidth + 20, yPos),
                Width = inputWidth,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            emotionsCombo.Items.AddRange(new[] { "Confident", "Nervous", "Excited", "Fearful", "Greedy", "Disciplined", "Impulsive", "Neutral" });
            mainPanel.Controls.Add(emotionsCombo);
            yPos += spacing;

            // Notes
            AddLabel(mainPanel, "Notes:", 10, yPos, labelWidth);
            notesInput = new TextBox
            {
                Location = new Point(labelWidth + 20, yPos),
                Width = inputWidth,
                Height = 80,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            mainPanel.Controls.Add(notesInput);
            yPos += 90;

            // Buttons
            saveButton = new Button
            {
                Text = "Save",
                Location = new Point(labelWidth - 50, yPos),
                Width = 100,
                Height = 35,
                BackColor = Color.FromArgb(50, 150, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            saveButton.FlatAppearance.BorderSize = 0;
            saveButton.Click += SaveButton_Click;
            mainPanel.Controls.Add(saveButton);

            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(labelWidth + 70, yPos),
                Width = 100,
                Height = 35,
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            cancelButton.FlatAppearance.BorderSize = 0;
            cancelButton.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            mainPanel.Controls.Add(cancelButton);

            this.Controls.Add(mainPanel);
        }

        private void AddLabel(Panel panel, string text, int x, int y, int width)
        {
            var label = new Label
            {
                Text = text,
                Location = new Point(x, y + 3),
                Width = width,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
            };
            panel.Controls.Add(label);
        }

        private TextBox AddTextBox(Panel panel, int x, int y, int width)
        {
            var textBox = new TextBox
            {
                Location = new Point(x, y),
                Width = width
            };
            panel.Controls.Add(textBox);
            return textBox;
        }

        private void LoadTradeData()
        {
            if (_trade == null) return;

            datePicker.Value = _trade.Date;
            symbolInput.Text = _trade.Symbol;
            outcomeCombo.Text = _trade.Outcome;
            typeCombo.Text = _trade.TradeType;
            modelInput.Text = _trade.Model;
            sessionInput.Text = _trade.Session;
            plInput.Text = _trade.PL.ToString("F2");
            rrInput.Text = _trade.RR.ToString("F2");
            entryTimeInput.Text = _trade.EntryTime;
            exitTimeInput.Text = _trade.ExitTime;
            entryPriceInput.Text = _trade.EntryPrice.ToString("F2");
            exitPriceInput.Text = _trade.ExitPrice.ToString("F2");
            contractsInput.Text = _trade.Contracts.ToString();
            feesInput.Text = _trade.Fees.ToString("F2");
            followedPlanCheckbox.Checked = _trade.FollowedPlan;
            emotionsCombo.Text = _trade.Emotions;
            notesInput.Text = _trade.Notes;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(symbolInput.Text))
            {
                MessageBox.Show("Please enter a symbol.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                symbolInput.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(outcomeCombo.Text))
            {
                MessageBox.Show("Please select an outcome.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                outcomeCombo.Focus();
                return;
            }

            // Update trade object
            _trade.Date = datePicker.Value;
            _trade.Symbol = symbolInput.Text.Trim();
            _trade.Outcome = outcomeCombo.Text;
            _trade.TradeType = typeCombo.Text;
            _trade.Model = modelInput.Text.Trim();
            _trade.Session = sessionInput.Text.Trim();
            _trade.Account = _accountNumber;
            _trade.EntryTime = entryTimeInput.Text.Trim();
            _trade.ExitTime = exitTimeInput.Text.Trim();
            _trade.FollowedPlan = followedPlanCheckbox.Checked;
            _trade.Emotions = emotionsCombo.Text;
            _trade.Notes = notesInput.Text.Trim();

            // Parse numeric fields
            if (decimal.TryParse(plInput.Text, out decimal pl))
                _trade.PL = pl;

            if (double.TryParse(rrInput.Text, out double rr))
                _trade.RR = rr;

            if (decimal.TryParse(entryPriceInput.Text, out decimal entryPrice))
                _trade.EntryPrice = entryPrice;

            if (decimal.TryParse(exitPriceInput.Text, out decimal exitPrice))
                _trade.ExitPrice = exitPrice;

            if (int.TryParse(contractsInput.Text, out int contracts))
                _trade.Contracts = contracts;

            if (decimal.TryParse(feesInput.Text, out decimal fees))
                _trade.Fees = fees;

            // Save to service
            if (_isEditMode)
            {
                TradingJournalService.Instance.UpdateTrade(_accountNumber, _trade);
            }
            else
            {
                TradingJournalService.Instance.AddTrade(_accountNumber, _trade);
                
                // Increment model usage count if a model was selected
                if (!string.IsNullOrWhiteSpace(_trade.Model))
                {
                    TradingJournalService.Instance.IncrementModelUsage(_accountNumber, _trade.Model);
                }
            }

            this.DialogResult = DialogResult.OK;
        }
    }
}
