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
using MarketPOS.Data;
using MarketPOS.Services;

namespace MarketPOS
{
    public partial class RefundWindow : Window
    {
        private DatabaseService _db;
        public bool RefundCompleted { get; private set; } = false;

        public RefundWindow()
        {
            InitializeComponent();
            _db = new DatabaseService();
            LoadRefundableTransactions();
        }

        private void LoadRefundableTransactions()
        {
            var list = _db.GetRefundableTransactions().ToList();
            CmbTransactions.ItemsSource = list;
        }

        private void CmbTransactions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbTransactions.SelectedItem is Transaction t)
            {
                // Detayları Getir
                var details = _db.GetTransactionDetails(t.Id).ToList();
                ListItems.ItemsSource = details;
                TxtRefundTotal.Text = $"{t.TotalAmount:N2} ₺";
            }
            else
            {
                ListItems.ItemsSource = null;
                TxtRefundTotal.Text = "0.00 ₺";
            }
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (CmbTransactions.SelectedItem is Transaction t)
            {
                if (ModernMsgBox.Show($"Fiş #{t.Id} iade edilecek.\nStoklar geri yüklenecek.\nOnaylıyor musunuz?", "İade Onayı", MessageBoxButton.YesNo, MessageBoxImage.Warning))
                {
                    try
                    {
                        _db.RefundSpecificTransaction(t.Id);
                        _db.AddLog(TxTypes.Iade, $"Manuel İade: Fiş #{t.Id}");
                        ModernMsgBox.Show("İade işlemi başarıyla tamamlandı.", "Başarılı");
                        RefundCompleted = true;
                        Close();
                    }
                    catch (Exception ex)
                    {
                        ToastNotification.Show("Hata: " + ex.Message, true);
                    }
                }
            }
            else
            {
                ModernMsgBox.Show("Lütfen önce listeden bir fiş seçin.", "Uyarı");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}