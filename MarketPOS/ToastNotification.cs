using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Media;
using System.Windows.Threading;
namespace MarketPOS
{
    public static class ToastNotification
    {
        public static void Show(string message, bool isError = false)
        {
            if (isError)
            {
                SystemSounds.Hand.Play();
            }
            else
            {
                SystemSounds.Asterisk.Play();
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                var win = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = Brushes.Transparent,
                    Topmost = true,
                    ShowActivated = false,
                    ShowInTaskbar = false,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    IsHitTestVisible = false,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Opacity = 0 // Position first, then show
                };

                var border = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isError ? "#EF4444" : "#10B981")),
                    CornerRadius = new CornerRadius(16),
                    Padding = new Thickness(60, 30, 60, 30),
                    Margin = new Thickness(20)
                };

                var text = new TextBlock
                {
                    Text = message,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 28,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                border.Child = text;
                win.Content = border;

                win.Loaded += (s, e) =>
                {
                    var workArea = SystemParameters.WorkArea;
                    win.Left = workArea.Right - win.ActualWidth - 20;
                    win.Top = workArea.Top + 20;
                    win.Opacity = 1;
                };

                win.Show();

                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2400) };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    win.Close();
                };
                timer.Start();
            });
        }
    }
}
