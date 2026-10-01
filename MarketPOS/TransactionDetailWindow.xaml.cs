using MarketPOS.Data;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MarketPOS
{
    public partial class TransactionDetailWindow : Window
    {
        public Transaction SelectedTransaction { get; set; }
        public List<TransactionDetailItem> Items { get; set; }

        public string TransactionColor => SelectedTransaction.Type == "İade" ? "#FEE2E2" : "#DCFCE7";

        // KAR VE CİRO ETKİSİ HESABI (RAPORLARLA AYNI MANTIK)
        public string ImpactInfo
        {
            get
            {
                var t = SelectedTransaction;
                decimal profit = 0;
                decimal revenue = 0;

                switch (t.Type)
                {
                    case "Satis":
                    case "Satış":
                        revenue = t.TotalAmount;
                        profit = t.TotalAmount - t.CostAmount;
                        break;
                    case "Veresiye":
                        // Veresiye: goods given on credit. No revenue until Tahsilat.
                        // Cost is a real loss at sale time.
                        revenue = 0;
                        profit = -t.CostAmount;
                        break;
                    case "Tahsilat":
                        // Tahsilat: collecting debt payment. This IS revenue and profit.
                        revenue = t.TotalAmount;
                        profit = t.TotalAmount;
                        break;
                    case "İade":
                    case "Iade":
                        revenue = t.TotalAmount; // negative
                        profit = t.TotalAmount - t.CostAmount; // negative impact
                        break;
                    case "Gider":
                        revenue = 0;
                        profit = t.TotalAmount; // already negative
                        break;
                    case "KasaGiris":
                        revenue = 0;
                        profit = 0; // cash entry is not profit
                        break;
                    default:
                        revenue = t.TotalAmount;
                        profit = 0;
                        break;
                }

                return $"Etki: Ciro {revenue:N2} \u20ba | Kar {profit:N2} \u20ba";
            }
        }

        public Brush ImpactColor
        {
            get
            {
                var text = ImpactInfo;
                if (text.Contains("Kar -")) return Brushes.Red;
                if (text.Contains("Kar 0.00")) return Brushes.Gray;
                return Brushes.Green;
            }
        }

        public TransactionDetailWindow(Transaction transaction, List<TransactionDetailItem> items)
        {
            InitializeComponent();
            SelectedTransaction = transaction;
            Items = items;
            DataContext = this;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}