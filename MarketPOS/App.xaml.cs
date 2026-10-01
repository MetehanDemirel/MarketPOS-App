using System.Configuration;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace MarketPOS
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // 1. Türkçe Kültürü Oluştur
            var cultureInfo = new CultureInfo("tr-TR");

            // 2. Para birimi sembolünü garantiye al (İsteğe bağlı ama önerilir)
            cultureInfo.NumberFormat.CurrencySymbol = "₺";

            // 3. Uygulamanın varsayılan kültürünü ayarla
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            // 4. WPF Framework elemanları için dil ayarı (Tarih formatları vs. için)
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            base.OnStartup(e);
        }
    }
}
