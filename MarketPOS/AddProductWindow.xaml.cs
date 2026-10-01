using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MarketPOS.Data;

namespace MarketPOS
{
    public partial class AddProductWindow : Window
    {
        public Product NewProduct { get; private set; }
        public bool IsSaved { get; private set; }

        public AddProductWindow(List<string> categories, string initialBarcode = null)
        {
            InitializeComponent();
            NewProduct = new Product { Stock = 999999, Price = 0, Cost = 0 };
            if (!string.IsNullOrEmpty(initialBarcode)) NewProduct.Barcode = initialBarcode;

            CmbCategories.ItemsSource = categories;
            DataContext = NewProduct;

            Loaded += (s, e) =>
            {
                TxtBarcode.Focus();
                TxtBarcode.SelectAll();
            };
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var focusElement = FocusManager.GetFocusedElement(this) as UIElement;
            focusElement?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

            if (string.IsNullOrWhiteSpace(NewProduct.Name) || string.IsNullOrWhiteSpace(NewProduct.Category))
            {
                ToastNotification.Show("Lütfen İsim ve Kategori giriniz.", true);
                return;
            }
            IsSaved = true;
            Close();
        }

        private void TxtBarcode_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            if (string.IsNullOrWhiteSpace(NewProduct.Barcode)) return;

            try
            {
                var db = new Services.DatabaseService();
                using var c = db.GetConnection();
                var exists = Dapper.SqlMapper.QueryFirstOrDefault<int?>(c,
                    "SELECT Id FROM Products WHERE Barcode = @B AND IsActive = 1",
                    new { B = NewProduct.Barcode });

                if (exists.HasValue)
                {
                    ToastNotification.Show("Bu barkod zaten sistemde kayıtlı!", true);
                }
                else
                {
                    ToastNotification.Show("Barkod kullanılabilir. Devam edebilirsiniz.");
                    (sender as TextBox)?.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                }
            }
            catch (Exception ex)
            {
                ToastNotification.Show("Hata: " + ex.Message, true);
            }
        }
    }
}