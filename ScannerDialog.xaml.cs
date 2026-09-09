using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace GHelperAutoProfileSwitcher
{
    // Modello dati per gestire le spunte nella lista
    public class ScannedExecutable
    {
        public bool IsSelected { get; set; }
        public string? FileName { get; set; }
        public string? FullPath { get; set; }
    }

    public partial class ScannerDialog : Window
    {
        public ObservableCollection<string> ScanFolders { get; set; }
        public ObservableCollection<ScannedExecutable> ScanResults { get; set; }
        public List<string> SelectedProcesses { get; private set; } = new List<string>();

        public ScannerDialog()
        {
            InitializeComponent();
            
            // Carica le cartelle di default dal nostro FolderScanner
            ScanFolders = new ObservableCollection<string>(FolderScanner.GetDefaultGameFolders());
            FoldersListBox.ItemsSource = ScanFolders;

            ScanResults = new ObservableCollection<ScannedExecutable>();
            ResultsListBox.ItemsSource = ScanResults;
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog folderDialog = new OpenFolderDialog();
            folderDialog.Title = "Select a custom game folder";
            
            if (folderDialog.ShowDialog() == true)
            {
                if (!ScanFolders.Contains(folderDialog.FolderName))
                {
                    ScanFolders.Add(folderDialog.FolderName);
                }
            }
        }

        private void RemoveFolder_Click(object sender, RoutedEventArgs e)
        {
            if (FoldersListBox.SelectedItem is string selectedFolder)
            {
                ScanFolders.Remove(selectedFolder);
            }
        }

        private void Scan_Click(object sender, RoutedEventArgs e)
        {
            ScanResults.Clear();
            var foundFiles = FolderScanner.ScanForGameExecutables(ScanFolders.ToList());

            foreach (var file in foundFiles)
            {
                ScanResults.Add(new ScannedExecutable
                {
                    IsSelected = false,
                    FileName = Path.GetFileNameWithoutExtension(file),
                    FullPath = file // Visibile passandoci sopra con il mouse
                });
            }
            
            if (ScanResults.Count == 0)
            {
                System.Windows.MessageBox.Show("No executables found in the selected folders.");
            }
        }

        private void AddSelected_Click(object sender, RoutedEventArgs e)
        {
            // Salva solo i file a cui l'utente ha messo la spunta
            foreach (var item in ScanResults.Where(x => x.IsSelected))
            {
                SelectedProcesses.Add(item.FileName);
            }
            
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}