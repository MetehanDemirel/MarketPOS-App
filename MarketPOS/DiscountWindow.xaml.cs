using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace MarketPOS
{
    public partial class DiscountWindow : Window
    {
        public int SelectedPercentage { get; private set; }
        private string _inputValue = "0";

        public DiscountWindow()
        {
            InitializeComponent();
            UpdateDisplay();
        }

        private void BtnNumpad_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string val = btn.Content.ToString();

                if (_inputValue == "0")
                    _inputValue = val;
                else
                    _inputValue += val;
                
                // Prevent > 100
                if (int.TryParse(_inputValue, out int num))
                {
                    if (num > 100) _inputValue = "100";
                }
                else
                {
                    _inputValue = "0";
                }

                UpdateDisplay();
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            _inputValue = "0";
            UpdateDisplay();
        }

        private void BtnBackspace_Click(object sender, RoutedEventArgs e)
        {
            if (_inputValue.Length > 1)
                _inputValue = _inputValue.Substring(0, _inputValue.Length - 1);
            else
                _inputValue = "0";
                
            UpdateDisplay();
        }
        
        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(_inputValue, out int amount) && amount > 0 && amount <= 100)
            {
                SelectedPercentage = amount;
                DialogResult = true;
                Close();
            }
            else
            {
                SelectedPercentage = 0;
                DialogResult = true;
                Close();
            }
        }

        private void UpdateDisplay()
        {
            TxtDisplay.Text = _inputValue;
        }
    }
}
