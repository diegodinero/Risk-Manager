using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Risk_Manager.Data
{
    /// <summary>
    /// Represents a single trade in the trading journal
    /// </summary>
    public class JournalTrade
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Date { get; set; } = DateTime.Today;
        public string Symbol { get; set; } = "";
        public string Outcome { get; set; } = ""; // Win/Loss/Breakeven
        public string TradeType { get; set; } = ""; // Long/Short
        public string Model { get; set; } = ""; // Trading model/strategy
        public string Session { get; set; } = ""; // Market session
        public decimal PL { get; set; } = 0m; // Profit/Loss
        public double RR { get; set; } = 0.0; // Risk/Reward ratio
        public string EntryTime { get; set; } = "";
        public string ExitTime { get; set; } = "";
        public decimal EntryPrice { get; set; } = 0m;
        public decimal ExitPrice { get; set; } = 0m;
        public int Contracts { get; set; } = 1;
        public string Account { get; set; } = "";
        public string Notes { get; set; } = "";
        public bool FollowedPlan { get; set; } = true;
        public string Emotions { get; set; } = ""; // How trader felt
        public decimal Fees { get; set; } = 0m;
        
        // Computed property
        public decimal NetPL => PL - Fees;
    }

    /// <summary>
    /// Service for managing trading journal entries with persistence
    /// </summary>
    public class TradingJournalService
    {
        private static TradingJournalService _instance;
        private static readonly object _lock = new object();
        
        private readonly string _dataDirectory;
        private readonly string _journalFilePath;
        private readonly string _notesFilePath;
        private Dictionary<string, List<JournalTrade>> _accountTrades; // Account number -> list of trades
        private Dictionary<string, List<JournalNote>> _accountNotes; // Account number -> list of notes

        private TradingJournalService()
        {
            _dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RiskManager",
                "Journal"
            );
            
            _journalFilePath = Path.Combine(_dataDirectory, "trading_journal.json");
            _notesFilePath = Path.Combine(_dataDirectory, "journal_notes.json");
            _accountTrades = new Dictionary<string, List<JournalTrade>>();
            _accountNotes = new Dictionary<string, List<JournalNote>>();
            
            LoadJournal();
            LoadNotes();
        }

        public static TradingJournalService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new TradingJournalService();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Load journal entries from JSON file
        /// </summary>
        private void LoadJournal()
        {
            try
            {
                if (!Directory.Exists(_dataDirectory))
                {
                    Directory.CreateDirectory(_dataDirectory);
                }

                if (File.Exists(_journalFilePath))
                {
                    var json = File.ReadAllText(_journalFilePath);
                    _accountTrades = JsonSerializer.Deserialize<Dictionary<string, List<JournalTrade>>>(json)
                                    ?? new Dictionary<string, List<JournalTrade>>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading journal: {ex.Message}");
                _accountTrades = new Dictionary<string, List<JournalTrade>>();
            }
        }

        /// <summary>
        /// Save journal entries to JSON file
        /// </summary>
        private void SaveJournal()
        {
            try
            {
                if (!Directory.Exists(_dataDirectory))
                {
                    Directory.CreateDirectory(_dataDirectory);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_accountTrades, options);
                File.WriteAllText(_journalFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving journal: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all trades for a specific account
        /// </summary>
        public List<JournalTrade> GetTrades(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber))
                return new List<JournalTrade>();

            if (!_accountTrades.ContainsKey(accountNumber))
            {
                _accountTrades[accountNumber] = new List<JournalTrade>();
            }

            // Return a copy sorted by date descending (most recent first)
            return _accountTrades[accountNumber]
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.EntryTime)
                .ToList();
        }

        /// <summary>
        /// Add a new trade to the journal
        /// </summary>
        public void AddTrade(string accountNumber, JournalTrade trade)
        {
            if (string.IsNullOrEmpty(accountNumber) || trade == null)
                return;

            if (!_accountTrades.ContainsKey(accountNumber))
            {
                _accountTrades[accountNumber] = new List<JournalTrade>();
            }

            // Ensure the trade has a unique ID
            if (trade.Id == Guid.Empty)
            {
                trade.Id = Guid.NewGuid();
            }

            _accountTrades[accountNumber].Add(trade);
            SaveJournal();
        }

        /// <summary>
        /// Update an existing trade
        /// </summary>
        public void UpdateTrade(string accountNumber, JournalTrade trade)
        {
            if (string.IsNullOrEmpty(accountNumber) || trade == null)
                return;

            if (!_accountTrades.ContainsKey(accountNumber))
                return;

            var existingTrade = _accountTrades[accountNumber].FirstOrDefault(t => t.Id == trade.Id);
            if (existingTrade != null)
            {
                var index = _accountTrades[accountNumber].IndexOf(existingTrade);
                _accountTrades[accountNumber][index] = trade;
                SaveJournal();
            }
        }

        /// <summary>
        /// Delete a trade from the journal
        /// </summary>
        public void DeleteTrade(string accountNumber, Guid tradeId)
        {
            if (string.IsNullOrEmpty(accountNumber))
                return;

            if (!_accountTrades.ContainsKey(accountNumber))
                return;

            var trade = _accountTrades[accountNumber].FirstOrDefault(t => t.Id == tradeId);
            if (trade != null)
            {
                _accountTrades[accountNumber].Remove(trade);
                SaveJournal();
            }
        }

        /// <summary>
        /// Get summary statistics for an account
        /// </summary>
        public JournalStats GetStats(string accountNumber)
        {
            var trades = GetTrades(accountNumber);
            if (trades.Count == 0)
            {
                return new JournalStats();
            }

            var wins = trades.Where(t => t.Outcome?.ToLower() == "win").ToList();
            var losses = trades.Where(t => t.Outcome?.ToLower() == "loss").ToList();
            var breakevens = trades.Where(t => t.Outcome?.ToLower() == "breakeven").ToList();

            return new JournalStats
            {
                TotalTrades = trades.Count,
                Wins = wins.Count,
                Losses = losses.Count,
                Breakevens = breakevens.Count,
                WinRate = trades.Count > 0 ? (double)wins.Count / trades.Count * 100 : 0,
                TotalPL = trades.Sum(t => t.NetPL),
                AveragePL = trades.Count > 0 ? trades.Average(t => t.NetPL) : 0,
                LargestWin = wins.Count > 0 ? wins.Max(t => t.NetPL) : 0,
                LargestLoss = losses.Count > 0 ? losses.Min(t => t.NetPL) : 0,
                AverageWin = wins.Count > 0 ? wins.Average(t => t.NetPL) : 0,
                AverageLoss = losses.Count > 0 ? losses.Average(t => t.NetPL) : 0
            };
        }

        // ============= NOTES MANAGEMENT =============

        /// <summary>
        /// Load notes from JSON file
        /// </summary>
        private void LoadNotes()
        {
            try
            {
                if (File.Exists(_notesFilePath))
                {
                    var json = File.ReadAllText(_notesFilePath);
                    _accountNotes = JsonSerializer.Deserialize<Dictionary<string, List<JournalNote>>>(json)
                                   ?? new Dictionary<string, List<JournalNote>>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading notes: {ex.Message}");
                _accountNotes = new Dictionary<string, List<JournalNote>>();
            }
        }

        /// <summary>
        /// Save notes to JSON file
        /// </summary>
        private void SaveNotes()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_accountNotes, options);
                File.WriteAllText(_notesFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving notes: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all notes for a specific account
        /// </summary>
        public List<JournalNote> GetNotes(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber))
                return new List<JournalNote>();

            if (!_accountNotes.ContainsKey(accountNumber))
                _accountNotes[accountNumber] = new List<JournalNote>();

            return _accountNotes[accountNumber].OrderByDescending(n => n.CreatedAt).ToList();
        }

        /// <summary>
        /// Add or update a note
        /// </summary>
        public void SaveNote(string accountNumber, JournalNote note)
        {
            if (string.IsNullOrEmpty(accountNumber) || note == null)
                return;

            if (!_accountNotes.ContainsKey(accountNumber))
                _accountNotes[accountNumber] = new List<JournalNote>();

            var existingNote = _accountNotes[accountNumber].FirstOrDefault(n => n.Id == note.Id);
            if (existingNote != null)
            {
                // Update existing
                existingNote.Title = note.Title;
                existingNote.Content = note.Content;
                existingNote.ImagePath = note.ImagePath;
            }
            else
            {
                // Add new
                note.Account = accountNumber;
                _accountNotes[accountNumber].Add(note);
            }

            SaveNotes();
        }

        /// <summary>
        /// Delete a note
        /// </summary>
        public void DeleteNote(string accountNumber, Guid noteId)
        {
            if (string.IsNullOrEmpty(accountNumber))
                return;

            if (_accountNotes.ContainsKey(accountNumber))
            {
                _accountNotes[accountNumber].RemoveAll(n => n.Id == noteId);
                SaveNotes();
            }
        }
    }

    /// <summary>
    /// Journal statistics summary
    /// </summary>
    public class JournalStats
    {
        public int TotalTrades { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Breakevens { get; set; }
        public double WinRate { get; set; }
        public decimal TotalPL { get; set; }
        public decimal AveragePL { get; set; }
        public decimal LargestWin { get; set; }
        public decimal LargestLoss { get; set; }
        public decimal AverageWin { get; set; }
        public decimal AverageLoss { get; set; }
    }

    /// <summary>
    /// Represents a trading journal note
    /// </summary>
    public class JournalNote
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string ImagePath { get; set; } = "";
        public string Account { get; set; } = "";
    }
}
