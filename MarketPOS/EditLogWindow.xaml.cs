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
using MarketPOS.Data;

namespace MarketPOS
{
    public partial class EditLogWindow : Window
    {
        public SystemLog Log { get; private set; }
        public bool IsSaved { get; private set; } = false;

        public EditLogWindow(SystemLog logToEdit)
        {
            InitializeComponent();
            // Logu kopyala ki iptal ederse ana liste bozulmasın
            Log = new SystemLog
            {
                Id = logToEdit.Id,
                Date = logToEdit.Date,
                ActionType = logToEdit.ActionType,
                Description = logToEdit.Description
            };
            DataContext = Log;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Log.Description)) { ModernMsgBox.Show("Açıklama boş olamaz.", "Hata"); return; }
            IsSaved = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) { Close(); }
    }
}