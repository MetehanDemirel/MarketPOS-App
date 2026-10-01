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

namespace MarketPOS
{
    public partial class ModernMsgBox : Window
    {
        public bool Result { get; private set; } = false; 

        public ModernMsgBox(string msg, string title, bool isConfirmation, bool isError)
        {
            InitializeComponent();
            TxtMessage.Text = msg;
            TxtTitle.Text = title;

            // 1. BUTON AYARLARI (Önce butonları ayarla)
            if (isConfirmation)
            {
                // Soru ise İptal/Hayır butonu GÖZÜKSÜN
                BtnCancel.Visibility = Visibility.Visible;
                BtnOk.Content = "EVET";
                BtnCancel.Content = "HAYIR";
            }
            else
            {
                // Düz bilgi ise sadece TAMAM butonu
                BtnCancel.Visibility = Visibility.Collapsed;
                BtnOk.Content = "TAMAM";
            }

            // 2. RENK AYARLARI (Buton durumundan bağımsız olarak renk belirle)
            if (isError) // Hata veya Uyarı (Kırmızı)
            {
                var redColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                HeaderBorder.Background = redColor;
                BtnOk.Background = redColor;
            }
            else if (isConfirmation) // Sadece Soru/Onay (Turuncu)
            {
                var orangeColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                HeaderBorder.Background = orangeColor;
                BtnOk.Background = orangeColor;
            }
            else // Başarılı/Bilgi (Yeşil)
            {
                var greenColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                HeaderBorder.Background = greenColor;
                BtnOk.Background = greenColor;
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            Close();
        }

        // --- KULLANIM KOLAYLIĞI İÇİN STATİK METOD ---
        public static bool Show(string msg, string title = "Bilgi", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Information)
        {
            // YesNo veya OKCancel ise "Onaylama" modudur
            bool isConfirm = (buttons == MessageBoxButton.YesNo || buttons == MessageBoxButton.OKCancel);

            // Error veya Warning ise "Hata" modudur (Kırmızı renk için)
            bool isError = (icon == MessageBoxImage.Error || icon == MessageBoxImage.Warning);

            var msgBox = new ModernMsgBox(msg, title, isConfirm, isError);
            msgBox.ShowDialog();
            return msgBox.Result;
        }
    }
}