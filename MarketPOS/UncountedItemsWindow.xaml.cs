using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MarketPOS.Services;
using MarketPOS.Data;

namespace MarketPOS
{
    public partial class UncountedItemsWindow : Window
    {
        private readonly DatabaseService _db = new DatabaseService();

        public UncountedItemsWindow(DateTime? start = null, DateTime? end = null)
        {
            InitializeComponent();
            DpStart.SelectedDate = start ?? DateTime.Now.Date;
            DpEnd.SelectedDate = end ?? DateTime.Now.Date;
            LoadData();
        }

        private void LoadData()
        {
            DateTime s = DpStart.SelectedDate ?? DateTime.Now.Date;
            DateTime e = (DpEnd.SelectedDate ?? DateTime.Now.Date).AddDays(1).AddTicks(-1);

            var items = _db.GetUncountedSoldItemsByDateRange(s, e).ToList();
            DgItems.ItemsSource = items;

            decimal total = items.Sum(x => x.TotalAmount);
            TxtGrandTotal.Text = total.ToString("N2") + " ₺";
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
