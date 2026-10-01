using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MarketPOS.Data;

namespace MarketPOS
{
    public partial class CategoryManagerWindow : Window
    {
        public Action<string> OnAddCategory;
        public Action<string> OnDeleteCategory;
        public Action<string, bool> OnToggleUncounted;
        private ObservableCollection<CategoryModel> _categories;

        public CategoryManagerWindow(List<CategoryModel> currentCategories)
        {
            InitializeComponent();
            _categories = new ObservableCollection<CategoryModel>(currentCategories);
            ListCategories.ItemsSource = _categories;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TxtCatName.Text))
            {
                OnAddCategory?.Invoke(TxtCatName.Text);
                _categories.Add(new CategoryModel { Name = TxtCatName.Text, IsUncounted = false });
                TxtCatName.Clear();
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string catName)
            {
                if (catName == "Genel" || catName == "TÜMÜ" || catName == "Gıda")
                {
                    ToastNotification.Show("Bu kategori silinemez.", true);
                    return;
                }

                if (ModernMsgBox.Show($"'{catName}' kategorisini ve İÇİNDEKİ ÜRÜNLERİ silmek üzeresiniz.\nEmin misiniz?", "Kritik Uyarı", MessageBoxButton.YesNo, MessageBoxImage.Warning))
                {
                    OnDeleteCategory?.Invoke(catName);
                    var item = _categories.FirstOrDefault(c => c.Name == catName);
                    if (item != null) _categories.Remove(item);
                }
            }
        }

        private void ChkUncounted_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox chk && chk.DataContext is CategoryModel cat)
            {
                OnToggleUncounted?.Invoke(cat.Name, cat.IsUncounted);
            }
        }
    }
}
