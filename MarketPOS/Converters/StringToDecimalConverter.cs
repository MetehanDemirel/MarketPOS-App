using System;
using System.Globalization;
using System.Windows.Data;

namespace MarketPOS.Converters
{
    public class StringToDecimalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal d)
            {
                return d == 0 ? "" : d.ToString("0.##", new CultureInfo("tr-TR"));
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = (value as string)?.Replace(".", ",").Trim();
            if (string.IsNullOrWhiteSpace(s)) return 0m;

            if (decimal.TryParse(s, NumberStyles.Any, new CultureInfo("tr-TR"), out decimal result))
            {
                return result;
            }
            return 0m;
        }
    }
}
