using MarketPOS.Data;
using MarketPOS.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MarketPOS.ViewModels
{
    public partial class MainViewModel
    {
        private ObservableCollection<Transaction> _reportTransactions = new ObservableCollection<Transaction>();
        public ObservableCollection<Transaction> ReportTransactions
        {
            get => _reportTransactions;
            set { _reportTransactions = value; OnPropertyChanged(); }
        }

        private ObservableCollection<SystemLog> _systemLogs = new ObservableCollection<SystemLog>();
        public ObservableCollection<SystemLog> SystemLogs
        {
            get => _systemLogs;
            set { _systemLogs = value; OnPropertyChanged(); }
        }

        public DateTime ReportStartDate { get; set; } = DateTime.Today;
        public DateTime ReportEndDate { get; set; } = DateTime.Today;

        private string _reportSearchFiNo;
        public string ReportSearchFiNo
        {
            get => _reportSearchFiNo;
            set { _reportSearchFiNo = value; OnPropertyChanged(); }
        }

        private decimal _reportRevenue;
        public decimal ReportRevenue { get => _reportRevenue; set { _reportRevenue = value; OnPropertyChanged(); } }

        private decimal _reportProfit;
        public decimal ReportProfit { get => _reportProfit; set { _reportProfit = value; OnPropertyChanged(); } }

        private decimal _reportCashInBox;
        public decimal ReportCashInBox { get => _reportCashInBox; set { _reportCashInBox = value; OnPropertyChanged(); } }

        private int _reportSalesCount;
        public int ReportSalesCount { get => _reportSalesCount; set { _reportSalesCount = value; OnPropertyChanged(); } }

        private decimal _currentMonthCiro;
        public decimal CurrentMonthCiro { get => _currentMonthCiro; set { _currentMonthCiro = value; OnPropertyChanged(); } }

        private decimal _currentWeekCiro;
        public decimal CurrentWeekCiro { get => _currentWeekCiro; set { _currentWeekCiro = value; OnPropertyChanged(); } }

        private decimal _uncountedRevenue;
        public decimal UncountedRevenue { get => _uncountedRevenue; set { _uncountedRevenue = value; OnPropertyChanged(); } }

        public DateTime LogStartDate { get; set; } = DateTime.Today;
        public DateTime LogEndDate { get; set; } = DateTime.Today;

        private int _currentReportPage = 1;
        public int CurrentReportPage { get => _currentReportPage; set { _currentReportPage = value; OnPropertyChanged(); } }
        private int _totalReportPages = 1;
        public int TotalReportPages { get => _totalReportPages; set { _totalReportPages = value; OnPropertyChanged(); } }

        private int _currentLogPage = 1;
        public int CurrentLogPage { get => _currentLogPage; set { _currentLogPage = value; OnPropertyChanged(); } }
        private int _totalLogPages = 1;
        public int TotalLogPages { get => _totalLogPages; set { _totalLogPages = value; OnPropertyChanged(); } }

        private List<Transaction> _fullReportTransactions = new List<Transaction>();
        private List<SystemLog> _fullLogs = new List<SystemLog>();

        private bool _hideFinancialLogs;
        public bool HideFinancialLogs
        {
            get => _hideFinancialLogs;
            set
            {
                _hideFinancialLogs = value;
                OnPropertyChanged();
                LoadLogs();
            }
        }

        private string _financialDesc;
        public string FinancialDesc { get => _financialDesc; set { _financialDesc = value; OnPropertyChanged(); } }

        private decimal _financialAmount;
        public decimal FinancialAmount { get => _financialAmount; set { _financialAmount = value; OnPropertyChanged(); } }

        public ICommand CalculateReportCommand { get; private set; }
        public ICommand AddExpenseCommand { get; private set; }
        public ICommand AddCashEntryCommand { get; private set; }
        public ICommand RefreshLogsCommand { get; private set; }
        public ICommand ShowTransactionDetailsCommand { get; private set; }
        public ICommand DeleteLogCommand { get; private set; }
        public ICommand OpenEditLogCommand { get; private set; }
        public ICommand OpenReceiptsWindowCommand { get; private set; }
        public ICommand OpenUncountedItemsWindowCommand { get; private set; }
        public ICommand OpenFastCashWindowCommand { get; private set; }

        public ICommand NextReportPageCommand { get; private set; }
        public ICommand PrevReportPageCommand { get; private set; }
        public ICommand NextLogPageCommand { get; private set; }
        public ICommand PrevLogPageCommand { get; private set; }

        private void InitializeReportCommands()
        {
            CalculateReportCommand = new RelayCommand(_ => GenerateReport());
            AddExpenseCommand = new RelayCommand(_ => AddFinancialTransaction(TxTypes.Gider));
            AddCashEntryCommand = new RelayCommand(_ => AddFinancialTransaction(TxTypes.KasaGiris));
            RefreshLogsCommand = new RelayCommand(_ => LoadLogs());
            ShowTransactionDetailsCommand = new RelayCommand(t => ShowTransactionDetails((Transaction)t));
            DeleteLogCommand = new RelayCommand(l => DeleteLog((SystemLog)l));
            OpenEditLogCommand = new RelayCommand(l => OpenEditLog((SystemLog)l));

            OpenReceiptsWindowCommand = new RelayCommand(_ => new ReceiptsWindow { DataContext = this }.Show());
            OpenFastCashWindowCommand = new RelayCommand(_ => new FastCashWindow { DataContext = this }.ShowDialog());
            OpenUncountedItemsWindowCommand = new RelayCommand(_ => new UncountedItemsWindow(ReportStartDate, ReportEndDate).Show());

            NextReportPageCommand = new RelayCommand(_ => { if (CurrentReportPage < TotalReportPages) { CurrentReportPage++; UpdateReportPage(); } });
            PrevReportPageCommand = new RelayCommand(_ => { if (CurrentReportPage > 1) { CurrentReportPage--; UpdateReportPage(); } });

            NextLogPageCommand = new RelayCommand(_ => { if (CurrentLogPage < TotalLogPages) { CurrentLogPage++; UpdateLogPage(); } });
            PrevLogPageCommand = new RelayCommand(_ => { if (CurrentLogPage > 1) { CurrentLogPage--; UpdateLogPage(); } });
        }

        private void GenerateReport()
        {
            DateTime s = ReportStartDate.Date;
            DateTime e = ReportEndDate.Date.AddDays(1).AddTicks(-1);

            var list = _db.GetTransactionsByDate(s, e).ToList();

            if (!string.IsNullOrEmpty(ReportSearchFiNo))
            {
                list = list.Where(x => x.Id.ToString() == ReportSearchFiNo).ToList();
            }

            _fullReportTransactions = list;
            CurrentReportPage = 1;
            UpdateReportPage();

            var uncountedCats = _db.GetUncountedCategoryNames();
            var allItems = _db.GetTransactionItemsByDateRange(s, e).ToList();

            // Build sets of transaction IDs by type so we can split items
            var satisIds = list.Where(x => x.Type == TxTypes.Satis).Select(x => x.Id).ToHashSet();
            var veresiyeIds = list.Where(x => x.Type == TxTypes.Veresiye).Select(x => x.Id).ToHashSet();
            var iadeDict = list.Where(x => x.Type == TxTypes.Iade).ToDictionary(x => x.Id);

            decimal uncountedTotal = 0m;
            decimal uncountedCost = 0m;
            // Only count items from actual cash/card sales (NOT Veresiye)
            decimal countedTotal = 0m;
            decimal countedCost = 0m;
            // Veresiye items tracked separately — cost is a real loss at sale time
            decimal veresiyeCost = 0m;

            foreach (var item in allItems)
            {
                decimal itemTotal = item.PriceAtSale * item.Quantity;
                decimal itemCost = item.CostAtSale * item.Quantity;

                bool isSatis = satisIds.Contains(item.TransactionId);
                bool isVeresiye = veresiyeIds.Contains(item.TransactionId);
                bool isIade = iadeDict.TryGetValue(item.TransactionId, out var iadeTx);

                if (isSatis)
                {
                    if (uncountedCats.Contains(item.Category))
                    {
                        uncountedTotal += itemTotal;
                        uncountedCost += itemCost;
                    }
                    else
                    {
                        countedTotal += itemTotal;
                        countedCost += itemCost;
                    }
                }
                else if (isVeresiye)
                {
                    // Veresiye: goods are given, cost is real. Revenue comes later via Tahsilat.
                    veresiyeCost += itemCost;
                }
                else if (isIade)
                {
                    if (iadeTx.PaymentMethod == PayMethods.Veresiye)
                    {
                        veresiyeCost -= itemCost;
                    }
                    else
                    {
                        if (uncountedCats.Contains(item.Category))
                        {
                            uncountedTotal -= itemTotal;
                            uncountedCost -= itemCost;
                        }
                        else
                        {
                            countedTotal -= itemTotal;
                            countedCost -= itemCost;
                        }
                    }
                }
            }

            UncountedRevenue = uncountedTotal;

            // --- KASA (Cash in register) ---
            decimal cashFromSales = list.Where(x => x.Type == TxTypes.Satis).Sum(x => x.ReceivedCash);
            decimal cashFromOthers = list.Where(x => x.Type == TxTypes.KasaGiris || x.Type == TxTypes.Tahsilat).Sum(x => x.TotalAmount);
            // For refunds: only count actual cash returned (not TotalAmount, which could be Veresiye refund with no cash)
            decimal cashOutExpense = list.Where(x => x.Type == TxTypes.Gider).Sum(x => Math.Abs(x.TotalAmount));
            decimal cashOutRefund = list.Where(x => x.Type == TxTypes.Iade).Sum(x => Math.Abs(x.ReceivedCash));

            ReportCashInBox = (cashFromSales + cashFromOthers) - cashOutExpense - cashOutRefund;

            // --- KAR (Profit) ---
            // Satis profit: revenue minus cost of goods sold (only real cash/card sales)
            decimal profitFromSales = countedTotal - countedCost;
            // Tahsilat: money collected for past Veresiye debts — this IS real profit
            // If we returned cash from a refunded Veresiye, it reverses Tahsilat profit
            decimal veresiyeCashRefund = list.Where(x => x.Type == TxTypes.Iade && x.PaymentMethod == PayMethods.Veresiye).Sum(x => Math.Abs(x.ReceivedCash));
            decimal profitTahsilat = list.Where(x => x.Type == TxTypes.Tahsilat).Sum(x => x.TotalAmount) - veresiyeCashRefund;
            // Veresiye cost: goods given on credit — cost is a real expense at sale time
            // (profit comes back via Tahsilat, so no double-counting)
            decimal lossVeresiye = veresiyeCost;
            // Expenses
            decimal lossExpense = list.Where(x => x.Type == TxTypes.Gider).Sum(x => Math.Abs(x.TotalAmount));

            ReportProfit = profitFromSales + profitTahsilat - lossVeresiye - lossExpense;

            // --- CİRO (Revenue) ---
            // Only actual cash/card sales + Tahsilat. (Refunds are already subtracted in countedTotal)
            decimal tahsilatTotal = list.Where(x => x.Type == TxTypes.Tahsilat).Sum(x => x.TotalAmount) - veresiyeCashRefund;
            
            ReportRevenue = countedTotal + tahsilatTotal;
            ReportSalesCount = list.Count(x => x.Type == TxTypes.Satis || x.Type == TxTypes.Veresiye);

            LoadQuickStats();
        }

        private void UpdateReportPage()
        {
            int pageSize = 50;
            TotalReportPages = Math.Max(1, (int)Math.Ceiling(_fullReportTransactions.Count / (double)pageSize));
            if (CurrentReportPage > TotalReportPages) CurrentReportPage = TotalReportPages;

            var chunk = _fullReportTransactions.Skip((CurrentReportPage - 1) * pageSize).Take(pageSize);
            ReportTransactions = new ObservableCollection<Transaction>(chunk);
        }

        private void LoadQuickStats()
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var startOfWeek = today.AddDays(-1 * diff).Date;
            var endOfToday = today.AddDays(1).AddTicks(-1);

            try 
            {
                CurrentMonthCiro = GetCiroForPeriod(startOfMonth, endOfToday);
                CurrentWeekCiro = GetCiroForPeriod(startOfWeek, endOfToday);
            }
            catch (Exception)
            {
            }
        }

        private decimal GetCiroForPeriod(DateTime start, DateTime end)
        {
            var items = _db.GetTransactionItemsByDateRange(start, end).ToList();
            var txs = _db.GetTransactionsByDate(start, end).ToList();
            var uncountedCats = _db.GetUncountedCategoryNames();
            
            var satisIds = txs.Where(x => x.Type == TxTypes.Satis).Select(x => x.Id).ToHashSet();
            var iadeDict = txs.Where(x => x.Type == TxTypes.Iade).ToDictionary(x => x.Id);
            
            decimal veresiyeCashRefund = txs.Where(x => x.Type == TxTypes.Iade && x.PaymentMethod == PayMethods.Veresiye).Sum(x => Math.Abs(x.ReceivedCash));
            decimal tahsilat = txs.Where(x => x.Type == TxTypes.Tahsilat).Sum(x => x.TotalAmount) - veresiyeCashRefund;
            
            decimal ciro = tahsilat;
            foreach(var item in items)
            {
                if (uncountedCats.Contains(item.Category)) continue;
                
                if (satisIds.Contains(item.TransactionId))
                {
                    ciro += item.PriceAtSale * item.Quantity;
                }
                else if (iadeDict.TryGetValue(item.TransactionId, out var tx) && tx.PaymentMethod != PayMethods.Veresiye)
                {
                    ciro -= item.PriceAtSale * item.Quantity;
                }
            }
            return ciro;
        }

        private void LoadLogs()
        {
            DateTime s = LogStartDate.Date;
            DateTime e = LogEndDate.Date.AddDays(1).AddTicks(-1);

            var l = _db.GetLogsByDate(s, e).ToList();

            if (HideFinancialLogs)
            {
                var f = new[] { TxTypes.Satis, TxTypes.Iade, TxTypes.Gider, TxTypes.KasaGiris, TxTypes.Veresiye, TxTypes.Tahsilat };
                l = l.Where(x => !f.Contains(x.ActionType)).ToList();
            }

            _fullLogs = l;
            CurrentLogPage = 1;
            UpdateLogPage();
        }

        private void UpdateLogPage()
        {
            int pageSize = 50;
            TotalLogPages = Math.Max(1, (int)Math.Ceiling(_fullLogs.Count / (double)pageSize));
            if (CurrentLogPage > TotalLogPages) CurrentLogPage = TotalLogPages;

            var chunk = _fullLogs.Skip((CurrentLogPage - 1) * pageSize).Take(pageSize);
            SystemLogs = new ObservableCollection<SystemLog>(chunk);
        }

        private void AddFinancialTransaction(string type)
        {
            if (FinancialAmount <= 0) return;

            _db.AddFinancialEntry(FinancialAmount, type, FinancialDesc ?? (type == TxTypes.Gider ? "Diğer" : "Kasa Giriş"));
            _db.AddLog(type, $"{type}: {FinancialAmount}");

            ModernMsgBox.Show("Kaydedildi", "Bilgi");
            FinancialAmount = 0;
            FinancialDesc = "";
        }

        private void ShowTransactionDetails(Transaction t)
        {
            if (t == null) return;
            var d = _db.GetTransactionDetails(t.Id).ToList();
            new TransactionDetailWindow(t, d).ShowDialog();
        }

        private void DeleteLog(SystemLog l)
        {
            if (l != null && ModernMsgBox.Show("Silinsin mi?", "Onay", MessageBoxButton.YesNo, MessageBoxImage.Warning))
            {
                _db.DeleteLog(l.Id);
                LoadLogs();
            }
        }

        private void OpenEditLog(SystemLog l)
        {
            if (l != null)
            {
                var w = new EditLogWindow(l);
                w.ShowDialog();
                if (w.IsSaved)
                {
                    _db.UpdateLog(w.Log);
                    LoadLogs();
                    ModernMsgBox.Show("Güncellendi", "Başarılı");
                }
            }
        }
    }
}
