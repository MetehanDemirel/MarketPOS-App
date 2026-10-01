using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using MarketPOS.ViewModels;

namespace MarketPOS
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => FocusBarcode();
        }

        private void FocusBarcode()
        {
            // Focus on Sales Barcode by default on load
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                if (TxtBarcode != null)
                {
                    TxtBarcode.Focus();
                    TxtBarcode.SelectAll();
                }
            }));
        }

        private void Window_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // PRO FEATURE: Global Input Capture
            // This allows the user to scan/type anywhere on the screen without manually clicking the search box.
            
            // 1. SAFETY: If the user is already typing in a TextBox (e.g. Price input, Customer Name), DO NOT INTERFERE.
            if (Keyboard.FocusedElement is TextBox) return;

            var vm = DataContext as MainViewModel;
            if (vm == null) return;

            TextBox target = null;

            // 2. SCOPING: Only capture input if we are on the relevant tab.
            // Index 0 = Sales Tab
            if (vm.SelectedTabIndex == 0) 
            {
                target = TxtBarcode;
            }
            // Index 1 = Inventory Tab
            else if (vm.SelectedTabIndex == 1) 
            {
                target = TxtInventory;
            }

            // If we are on Reports, Logs, etc., 'target' remains null, so we do nothing.

            if (target != null)
            {
                e.Handled = true; // Stop the event from doing anything else (like triggering hotkeys)
                target.Focus();   // Jump to the box
                target.Text += e.Text; // append the character
                target.CaretIndex = target.Text.Length; // Move cursor to end
            }
        }

        private void BtnQuick_Click(object sender, RoutedEventArgs e)
        {
             var w = new QuickMoneyWindow();
             if (w.ShowDialog() == true)
             {
                 var vm = DataContext as MainViewModel;
                 if (vm?.AddQuickMoneyCommand?.CanExecute(w.SelectedAmount) == true)
                 {
                     vm.AddQuickMoneyCommand.Execute(w.SelectedAmount);
                 }
             }
        }

        private void BtnDiscount_Click(object sender, RoutedEventArgs e)
        {
             var w = new DiscountWindow();
             if (w.ShowDialog() == true && w.SelectedPercentage > 0)
             {
                 var vm = DataContext as MainViewModel;
                 if (vm?.AddDiscountCommand?.CanExecute(w.SelectedPercentage) == true)
                 {
                     vm.AddDiscountCommand.Execute(w.SelectedPercentage);
                 }
             }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
