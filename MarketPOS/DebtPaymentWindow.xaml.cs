    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MarketPOS.Services;
using MarketPOS.Data;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MarketPOS
{
    public partial class DebtPaymentWindow : Window, INotifyPropertyChanged
    {
        public decimal PaymentAmount { get; private set; }
        public bool IsConfirmed { get; private set; } = false;
        public bool NeedsRefresh { get; private set; } = false; // To tell caller to refresh if partial payments made

        private Customer _customer;
        private DatabaseService _db = new DatabaseService();

        public string CustomerName => _customer.Name;
        public decimal CurrentDebt => _customer.DebtBalance;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public DebtPaymentWindow(Customer customer)
        {
            InitializeComponent();
            _customer = customer;
            DataContext = this;
            LoadReceipts();
        }

        private void LoadReceipts()
        {
            var unpaid = _db.GetUnpaidReceiptsByCustomerId(_customer.Id);
            ListUnpaidReceipts.ItemsSource = unpaid;
        }

        private void BtnPayAll_Click(object sender, RoutedEventArgs e)
        {
            TxtAmount.Text = _customer.DebtBalance.ToString("0.00");
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(TxtAmount.Text, out decimal amount) && amount > 0)
            {
                if (amount > _customer.DebtBalance)
                {
                    ToastNotification.Show("Ödenen tutar borçtan büyük olamaz.", true);
                    return;
                }
                PaymentAmount = amount;
                IsConfirmed = true;
                Close();
            }
            else
            {
                ToastNotification.Show("Geçerli bir tutar girin.", true);
            }
        }

        private void BtnViewReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Transaction t)
            {
                var transaction = _db.GetTransactionById(t.Id);
                if (transaction != null)
                {
                    var details = _db.GetTransactionDetails(t.Id) as List<TransactionDetailItem> ?? new List<TransactionDetailItem>((IEnumerable<TransactionDetailItem>)_db.GetTransactionDetails(t.Id));
                    var detailWin = new TransactionDetailWindow(transaction, details);
                    detailWin.ShowDialog();
                }
            }
        }

        private void BtnPayReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Transaction t)
            {
                decimal paymentAmount = _customer.DebtBalance > t.TotalAmount ? t.TotalAmount : _customer.DebtBalance;
                if (paymentAmount < 0) paymentAmount = 0;

                // 1. Mark as Paid (always, so it disappears from the list)
                _db.MarkTransactionAsPaid(t.Id);
                
                if (paymentAmount > 0)
                {
                    // 2. Add Payment Transaction for the profit/cash flow
                    using var cn = _db.GetConnection();
                    Dapper.SqlMapper.Execute(cn, 
                        "INSERT INTO Transactions (Date, TotalAmount, CostAmount, PaymentMethod, Type, Description, CustomerId) VALUES (@D, @T, 0, 'Nakit', 'Tahsilat', @Desc, @Ci)", 
                        new { D = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), T = paymentAmount, Desc = $"Tahsilat (Fiş #{t.Id})", Ci = _customer.Id });

                    // 3. Update Customer Balance
                    _db.UpdateCustomerDebt(_customer.Id, -paymentAmount);
                    _db.AddLog(TxTypes.Tahsilat, $"{_customer.Name} - {paymentAmount:C2} (Fiş #{t.Id})");

                    // 4. Update memory & UI
                    _customer.DebtBalance -= paymentAmount;
                    OnPropertyChanged(nameof(CurrentDebt));
                }

                LoadReceipts();
                NeedsRefresh = true;

                if (paymentAmount > 0)
                    ToastNotification.Show($"Fiş #{t.Id} kapatıldı ({paymentAmount:C2} tahsil edildi).");
                else
                    ToastNotification.Show($"Fiş #{t.Id} kapatıldı (Borç zaten sıfırdı).");
            }
        }
    }
}

