using System.Windows;

namespace MarketPOS
{
    public partial class ReceiptsWindow : Window
    {
        public ReceiptsWindow()
        {
            InitializeComponent();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
