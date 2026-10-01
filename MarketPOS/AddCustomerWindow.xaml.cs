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

namespace MarketPOS
{
    public partial class AddCustomerWindow : Window
    {
        public Customer NewCustomer { get; private set; }
        public bool IsSaved { get; private set; } = false;

        public AddCustomerWindow()
        {
            InitializeComponent();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text)) { ToastNotification.Show("İsim girmelisiniz.", true); return; }

            decimal balance = 0;
            decimal.TryParse(TxtBalance.Text, out balance);

            NewCustomer = new Customer
            {
                Name = TxtName.Text,
                Phone = TxtPhone.Text,
                DebtBalance = balance
            };
            IsSaved = true;
            Close();
        }
    }
}