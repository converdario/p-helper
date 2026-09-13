using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace PHelper
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // Apre il link nel browser predefinito di Windows
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}