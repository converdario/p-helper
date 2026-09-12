using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace PHelper
{
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
        
        public List<ScannedExecutable> SelectedExecutables { get; private set; } = new List<ScannedExecutable>();
        public List<ScannedExecutable> UnselectedExecutables { get; private set; } = new List<ScannedExecutable>();
        
        private List<string> _existingProcesses;

        public ScannerDialog(List<string> existingProcesses)
        {
            InitializeComponent();
            
            _existingProcesses = existingProcesses ?? new List<string>();
            
            var allFolders = FolderScanner.GetDefaultGameFolders();
            var config = ConfigManager.LoadConfig();
            
            foreach (var folder in config.CustomScanFolders)
            {
                if (!allFolders.Contains(folder)) 
                {
                    allFolders.Add(folder);
                }
            }
            
            ScanFolders = new ObservableCollection<string>(allFolders);
            FoldersListBox.ItemsSource = ScanFolders;

            ScanResults = new ObservableCollection<ScannedExecutable>();
            ResultsListBox.ItemsSource = ScanResults;

            this.Loaded += (s, e) => PerformScan();
        }

        private void PerformScan()
        {
            var currentlyChecked = ScanResults.Where(x => x.IsSelected).Select(x => x.FileName).ToList();
            
            ScanResults.Clear();
            var foundFiles = FolderScanner.ScanForGameExecutables(ScanFolders.ToList());

            foreach (var file in foundFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                
                bool shouldBeSelected = currentlyChecked.Contains(fileName) || 
                                        _existingProcesses.Contains(fileName, System.StringComparer.OrdinalIgnoreCase);

                ScanResults.Add(new ScannedExecutable
                {
                    IsSelected = shouldBeSelected,
                    FileName = fileName,
                    FullPath = file 
                });
            }
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
                    
                    var config = ConfigManager.LoadConfig();
                    if (!config.CustomScanFolders.Contains(folderDialog.FolderName))
                    {
                        config.CustomScanFolders.Add(folderDialog.FolderName);
                        ConfigManager.SaveConfig(config);
                    }
                    
                    PerformScan(); 
                }
            }
        }

        private void RemoveFolder_Click(object sender, RoutedEventArgs e)
        {
            if (FoldersListBox.SelectedItem is string selectedFolder)
            {
                ScanFolders.Remove(selectedFolder);
                
                var config = ConfigManager.LoadConfig();
                if (config.CustomScanFolders.Contains(selectedFolder))
                {
                    config.CustomScanFolders.Remove(selectedFolder);
                    ConfigManager.SaveConfig(config);
                }
                
                PerformScan(); 
            }
        }

        private void AddSelected_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in ScanResults)
            {
                if (item.IsSelected) SelectedExecutables.Add(item);
                else UnselectedExecutables.Add(item);
            }
            DialogResult = true;
            Close();
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
}