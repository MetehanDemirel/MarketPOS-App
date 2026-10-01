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

using System.Collections.Generic;
using System.Linq;
using MarketPOS.Data;

namespace MarketPOS
{
    public partial class CustomerSelectionWindow : Window
    {
        private List<Customer> _allCustomers;
        public Customer? SelectedCustomer { get; private set; }

        public CustomerSelectionWindow(List<Customer> customers)
        {
            InitializeComponent();
            _allCustomers = customers;
            ListCustomers.ItemsSource = _allCustomers;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var filter = TxtSearch.Text.ToLower();
            ListCustomers.ItemsSource = _allCustomers.Where(c => c.Name.ToLower().Contains(filter) || c.Phone.Contains(filter)).ToList();
        }

        // Satırdaki "SEÇ" butonuna basınca çalışır
        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Customer c)
            {
                SelectedCustomer = c;
                Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}