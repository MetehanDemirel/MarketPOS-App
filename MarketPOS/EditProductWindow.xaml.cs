using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using MarketPOS.Data;

namespace MarketPOS
{
    public partial class EditProductWindow : Window
    {
        public Product Product { get; private set; }
        public bool IsSaved { get; private set; }

        public EditProductWindow(Product productToEdit, List<string> categories)
        {
            InitializeComponent();
            Product = new Product
            {
                Id = productToEdit.Id,
                Barcode = productToEdit.Barcode,
                Name = productToEdit.Name,
                Category = productToEdit.Category,
                Price = productToEdit.Price,
                Cost = productToEdit.Cost,
                Stock = productToEdit.Stock
            };
            CmbCategories.ItemsSource = categories;
            DataContext = Product;
        }

        private void BtnStockInc_Click(object sender, RoutedEventArgs e) => Product.Stock++;
        private void BtnStockDec_Click(object sender, RoutedEventArgs e) => Product.Stock--;

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var focusElement = FocusManager.GetFocusedElement(this) as UIElement;
            focusElement?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

            if (string.IsNullOrWhiteSpace(Product.Name) || Product.Price < 0)
            {
                ModernMsgBox.Show("Geçersiz bilgi.", "Hata");
                return;
            }
            IsSaved = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}