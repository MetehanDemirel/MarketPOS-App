using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MarketPOS
{
    public partial class PaymentWindow : Window
    {
        private static readonly CultureInfo TrCulture = new("tr-TR");

        public decimal TotalAmount { get; set; }
        public decimal ResultCash { get; private set; }
        public decimal ResultCard { get; private set; }
        public bool IsCompleted { get; private set; }

        private bool _isUpdating;
        private bool _skipGotFocus;

        public PaymentWindow(decimal total)
        {
            InitializeComponent();
            TotalAmount = total;
            DataContext = this;
            TxtCash.Text = "";
            TxtCard.Text = "";
            Calculate();
        }

        private void BtnQuickMoney_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && decimal.TryParse(btn.Tag.ToString(), NumberStyles.Any, TrCulture, out decimal amount))
            {
                _skipGotFocus = true;
                TxtCash.Focus();
                _skipGotFocus = false;

                decimal.TryParse(TxtCash.Text, NumberStyles.Any, TrCulture, out decimal currentCash);
                currentCash += amount;

                TxtCash.Text = currentCash.ToString("0.##", TrCulture);
                TxtCash.CaretIndex = TxtCash.Text.Length;
            }
        }

        private void BtnClearCash_Click(object sender, RoutedEventArgs e)
        {
            _skipGotFocus = true;
            _isUpdating = true;
            TxtCash.Text = "";
            _isUpdating = false;
            TxtCash.Focus();
            _skipGotFocus = false;
            Calculate();
        }

        private void BtnClearCard_Click(object sender, RoutedEventArgs e)
        {
            _skipGotFocus = true;
            _isUpdating = true;
            TxtCard.Text = "";
            _isUpdating = false;
            TxtCard.Focus();
            _skipGotFocus = false;
            Calculate();
        }

        private void BtnExactCash_Click(object sender, RoutedEventArgs e)
        {
            _isUpdating = true;
            TxtCard.Text = "";
            TxtCash.Text = TotalAmount.ToString("0.##", TrCulture);
            _isUpdating = false;
            Calculate();
        }

        private void BtnFillCard_Click(object sender, RoutedEventArgs e)
        {
            decimal.TryParse(TxtCash.Text, NumberStyles.Any, TrCulture, out decimal cash);
            decimal remaining = Math.Max(0, TotalAmount - cash);

            _isUpdating = true;
            TxtCard.Text = remaining.ToString("0.##", TrCulture);
            _isUpdating = false;
            Calculate();
        }

        private void TxtInput_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_skipGotFocus) return;
            if (sender is TextBox tb) tb.Text = "";
        }

        private void TxtCash_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isUpdating) Calculate();
        }

        private void TxtCard_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isUpdating) Calculate();
        }

        private void Calculate()
        {
            decimal.TryParse(TxtCash.Text, NumberStyles.Any, TrCulture, out decimal cash);
            decimal.TryParse(TxtCard.Text, NumberStyles.Any, TrCulture, out decimal card);

            decimal totalPaid = cash + card;
            decimal difference = totalPaid - TotalAmount;

            if (difference >= -0.01m)
            {
                LblStatusTitle.Text = "PARA ÜSTÜ";
                LblStatusTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                LblRemaining.Text = $"{difference:N2} ₺";
                LblRemaining.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));

                BtnConfirm.IsEnabled = true;
                BtnConfirm.Opacity = 1;
                BtnConfirm.Content = "ÖDEMEYİ TAMAMLA";
                BtnConfirm.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
            }
            else
            {
                LblStatusTitle.Text = "KALAN TUTAR";
                LblStatusTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                LblRemaining.Text = $"{Math.Abs(difference):N2} ₺";
                LblRemaining.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));

                BtnConfirm.IsEnabled = false;
                BtnConfirm.Opacity = 0.5;
                BtnConfirm.Content = "EKSİK TUTAR";
                BtnConfirm.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
            }
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            decimal.TryParse(TxtCash.Text, NumberStyles.Any, TrCulture, out decimal cash);
            decimal.TryParse(TxtCard.Text, NumberStyles.Any, TrCulture, out decimal card);

            if (cash + card > TotalAmount)
            {
                decimal change = (cash + card) - TotalAmount;
                cash -= change;
            }

            ResultCash = cash;
            ResultCard = card;
            IsCompleted = true;
            Close();
        }
    }
}