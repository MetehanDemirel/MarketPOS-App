using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace MarketPOS
{
    public partial class QuickMoneyWindow : Window
    {
        public decimal SelectedAmount { get; private set; }
        private string _inputValue = "0";

        public QuickMoneyWindow()
        {
            InitializeComponent();
            UpdateDisplay();
        }

        private void BtnNumpad_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string val = btn.Content.ToString();

                if (val == ",") 
                {
                    if (!_inputValue.Contains(",")) 
                        _inputValue += ",";
                }
                else
                {
                    if (_inputValue == "0" && val != "00")
                        _inputValue = val;
                    else if (_inputValue == "0" && val == "00")
                        _inputValue = "0";
                    else
                        _inputValue += val;
                }
                
                // Prevent ridiculously large inputs arbitrarily breaking layout or parsing
                if (_inputValue.Length > 12)
                    _inputValue = _inputValue.Substring(0, 12);

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
            if (decimal.TryParse(_inputValue, NumberStyles.Any, new CultureInfo("tr-TR"), out decimal amount) && amount > 0)
            {
                SelectedAmount = amount;
                DialogResult = true;
                Close();
            }
            else
            {
                SelectedAmount = 0;
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
