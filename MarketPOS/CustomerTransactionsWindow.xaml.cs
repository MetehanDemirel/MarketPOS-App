using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MarketPOS.Data;
using MarketPOS.Services;

namespace MarketPOS
{
    public partial class CustomerTransactionsWindow : Window
    {
        private Customer _customer;
        private DatabaseService _db;

        public CustomerTransactionsWindow(Customer customer)
        {
            InitializeComponent();
            _customer = customer;
            _db = new DatabaseService();

            TxtCustomerName.Text = _customer.Name;
            LoadTransactions();
        }

        private void LoadTransactions()
        {
            try
            {
                var list = _db.GetTransactionsByCustomerId(_customer.Id);
                ListTransactions.ItemsSource = list;
            }
            catch (Exception ex)
            {
                ModernMsgBox.Show("Hata: " + ex.Message, "Hata");
            }
        }

        private void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int transId)
            {
                try
                {
                    var transaction = _db.GetTransactionById(transId);
                    if (transaction == null) return;

                    var details = _db.GetTransactionDetails(transId) as List<TransactionDetailItem> ?? new List<TransactionDetailItem>((IEnumerable<TransactionDetailItem>)_db.GetTransactionDetails(transId));

                    var detailWin = new TransactionDetailWindow(transaction, details);
                    detailWin.ShowDialog();
                }
                catch (Exception ex)
                {
                    ModernMsgBox.Show("Detaylar yüklenemedi: " + ex.Message, "Hata");
                }
            }
        }



        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
