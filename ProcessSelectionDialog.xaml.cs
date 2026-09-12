using System.Collections.Generic;
using System.Windows;
using System.Linq;

namespace PHelper
{
    public partial class ProcessSelectionDialog : Window
    {
        public List<ProcessInfo> SelectedProcesses { get; private set; } = new List<ProcessInfo>();
        public List<ProcessInfo> UnselectedProcesses { get; private set; } = new List<ProcessInfo>();

        public ProcessSelectionDialog(List<ProcessInfo> processes)
        {
            InitializeComponent();
            ProcessListBox.ItemsSource = processes;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Executable files (*.exe)|*.exe";
            openFileDialog.Title = "Select executable file";

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedProcesses.Add(new ProcessInfo 
                { 
                    ProcessName = System.IO.Path.GetFileNameWithoutExtension(openFileDialog.FileName),
                    FullPath = openFileDialog.FileName,
                    IsSelected = true
                });
                DialogResult = true;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            // Separiamo chi ha la spunta e chi non ce l'ha
            if (ProcessListBox.ItemsSource is List<ProcessInfo> items)
            {
                foreach (var item in items)
                {
                    if (item.IsSelected)
                        SelectedProcesses.Add(item);
                    else
                        UnselectedProcesses.Add(item);
                }
            }
            
            // Chiude senza errori anche se non si è selezionato nulla
            DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                this.DragMove();
            }
        }
    }

    public class ProcessInfo
    {
        public bool IsSelected { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string WindowTitle { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty; 
    }
}