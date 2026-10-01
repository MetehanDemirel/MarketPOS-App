using MarketPOS.Data;
using MarketPOS.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Dapper;

namespace MarketPOS.ViewModels
{
    public partial class MainViewModel
    {
        // --- Properties ---
        public ObservableCollection<Customer> Customers { get; set; } = new ObservableCollection<Customer>();
        private ObservableCollection<Customer> _displayedCustomers = new ObservableCollection<Customer>();
        public ObservableCollection<Customer> DisplayedCustomers
        {
            get => _displayedCustomers;
            set { _displayedCustomers = value; OnPropertyChanged(); }
        }
        
        private List<Customer> _allCustomersCache = new List<Customer>();

        private string _customerSearchInput;
        public string CustomerSearchInput
        {
            get => _customerSearchInput;
            set
            {
                _customerSearchInput = value;
                OnPropertyChanged();
                FilterCustomers();
            }
        }

        // --- Commands ---
        public ICommand OpenAddCustomerCommand { get; private set; }
        public ICommand PayDebtCommand { get; private set; }
        public ICommand DeleteCustomerCommand { get; private set; }
        public ICommand OpenCustomerHistoryCommand { get; private set; }

        // --- Methods ---
        private void InitializeCustomerCommands()
        {
            OpenAddCustomerCommand = new RelayCommand(_ => OpenAddCustomer());
            PayDebtCommand = new RelayCommand(c => PayDebt((Customer)c));
            DeleteCustomerCommand = new RelayCommand(c => DeleteCustomer((Customer)c));

            OpenCustomerHistoryCommand = new RelayCommand(c => 
            {
                if (c is Customer customer)
                {
                    var w = new CustomerTransactionsWindow(customer);
                    w.ShowDialog();
                    // Bakiyeler değişmiş olabilir, listeyi yenile
                    LoadData();
                }
            });
        }

        private void FilterCustomers()
        {
            var q = CustomerSearchInput?.ToLower() ?? "";
            var r = _allCustomersCache.AsEnumerable();

            if (!string.IsNullOrEmpty(q))
            {
                r = r.Where(x => x.Name.ToLower().Contains(q) || x.Phone.Contains(q));
            }

            DisplayedCustomers = new ObservableCollection<Customer>(r.ToList());
        }

        private void OpenAddCustomer()
        {
            var w = new AddCustomerWindow();
            w.ShowDialog();

            if (w.IsSaved)
            {
                int id = _db.AddCustomer(w.NewCustomer);
                
                if (w.NewCustomer.DebtBalance > 0)
                {
                    _db.AddTransaction(new Transaction 
                    { 
                        Date = DateTime.Now, 
                        TotalAmount = w.NewCustomer.DebtBalance, 
                        ReceivedCash = 0,
                        ReceivedCard = 0,
                        Type = TxTypes.Veresiye, 
                        Description = "Açılış Bakiyesi", 
                        CustomerId = id 
                    });
                    
                    _db.AddLog(LogTypes.MusteriEkleme, $"{w.NewCustomer.Name} (Açılış: {w.NewCustomer.DebtBalance})");
                }
                else
                {
                    _db.AddLog(LogTypes.MusteriEkleme, w.NewCustomer.Name);
                }

                LoadData();
                ModernMsgBox.Show("Müşteri Kaydedildi.", "Başarılı");
            }
        }

        private void PayDebt(Customer c)
        {
            var w = new DebtPaymentWindow(c);
            w.ShowDialog();
            
            if (w.IsConfirmed)
            {
                using var cn = _db.GetConnection();
                cn.Open();
                using var tran = cn.BeginTransaction();
                try
                {
                    Dapper.SqlMapper.Execute(cn, 
                        "INSERT INTO Transactions (Date, TotalAmount, CostAmount, PaymentMethod, Type, Description, CustomerId) VALUES (@D, @T, 0, @Pm, @Ty, @Desc, @Ci)", 
                        new { D = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), T = w.PaymentAmount, Pm = PayMethods.Nakit, Ty = TxTypes.Tahsilat, Desc = $"Tahsilat: {c.Name}", Ci = c.Id }, tran);

                    Dapper.SqlMapper.Execute(cn, 
                        "UPDATE Customers SET DebtBalance = DebtBalance - @A WHERE Id = @Id", 
                        new { A = w.PaymentAmount, Id = c.Id }, tran);

                    // FIFO: Mark oldest receipts as paid
                    var unpaid = cn.Query<Transaction>($"SELECT * FROM Transactions WHERE CustomerId = @Id AND Type = '{TxTypes.Veresiye}' AND IsPaid = 0 ORDER BY Id ASC", new { Id = c.Id }, tran).ToList();
                    decimal remainingPayment = w.PaymentAmount;
                    
                    foreach (var u in unpaid)
                    {
                        if (remainingPayment >= u.TotalAmount && u.TotalAmount > 0)
                        {
                            Dapper.SqlMapper.Execute(cn, "UPDATE Transactions SET IsPaid = 1 WHERE Id = @Id", new { Id = u.Id }, tran);
                            remainingPayment -= u.TotalAmount;
                        }
                        else if (remainingPayment > 0)
                        {
                            // Kısmi ödeme yapıldıysa, bu fişi tam kapatamıyoruz
                            // Ama kalan ödeme tutarı da bitti.
                            remainingPayment = 0;
                            break;
                        }
                    }
                    
                    tran.Commit();

                    _db.AddLog(LogTypes.Tahsilat, $"{c.Name} - {w.PaymentAmount:C2}");
                    ToastNotification.Show($"Tahsilat Alındı: {w.PaymentAmount:C2}");
                    LoadData();
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    ToastNotification.Show("Hata: " + ex.Message, true);
                }
            }
            else if (w.NeedsRefresh)
            {
                LoadData();
            }
        }

        private void DeleteCustomer(Customer c)
        {
            if (ModernMsgBox.Show("Silinsin mi?", "Onay", MessageBoxButton.YesNo, MessageBoxImage.Warning))
            {
                _db.DeleteCustomer(c.Id);
                LoadData();
            }
        }
    }
}
