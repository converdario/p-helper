using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using ComboBox = System.Windows.Controls.ComboBox;

namespace PHelper
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<AppProfile> _profiles;
        private TargetMode _defaultMode = TargetMode.Balanced;
        private DispatcherTimer _timer;
        private NotifyIcon? _notifyIcon;
        private TargetMode _currentMode = TargetMode.Balanced;
        private IntPtr _currentIconHandle = IntPtr.Zero;

        private bool _isPaused = false;
        private DateTime? _pauseUntil = null;
        private ToolStripMenuItem? _pauseMenuItem;
        private ToolStripMenuItem? _resumeMenuItem;

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        private void UpdateTrayIcon()
        {
            System.Drawing.Color accentColor;

            if (_isPaused)
            {
                accentColor = System.Drawing.ColorTranslator.FromHtml("#808080");
            }
            else
            {
                switch (_currentMode)
                {
                    case TargetMode.Silent: 
                        accentColor = System.Drawing.ColorTranslator.FromHtml("#10E659");
                        break;
                    case TargetMode.Balanced: 
                        accentColor = System.Drawing.ColorTranslator.FromHtml("#00A8FF"); 
                        break;
                    case TargetMode.Turbo: 
                        accentColor = System.Drawing.ColorTranslator.FromHtml("#FF4343"); 
                        break;
                    default:
                        accentColor = System.Drawing.ColorTranslator.FromHtml("#FF4343");
                        break;
                }
            }

            System.Drawing.Icon newIcon = IconGenerator.CreateTrayIcon(accentColor);

            var oldIcon = _notifyIcon!.Icon;
            _notifyIcon.Icon = newIcon;
            
            string statusText = _isPaused ? "Paused" : _currentMode.ToString();
            _notifyIcon.Text = $"PHelper - {statusText}";

            if (_currentIconHandle != IntPtr.Zero)
            {
                DestroyIcon(_currentIconHandle);
            }
            if (oldIcon != null && oldIcon != SystemIcons.Application)
            {
                oldIcon.Dispose();
            }
            
            _currentIconHandle = newIcon.Handle;
        }

        public MainWindow()
        {
            InitializeComponent();

            var config = ConfigManager.LoadConfig();
            _defaultMode = config.DefaultMode;
            _profiles = new ObservableCollection<AppProfile>(config.Profiles);
            ProfilesGrid.ItemsSource = _profiles;
            _profiles.CollectionChanged += (s, e) => SaveConfigSilently();

            DefaultModeComboBox.ItemsSource = Enum.GetValues(typeof(TargetMode));
            DefaultModeComboBox.SelectedItem = _defaultMode;

            SetupTrayIcon();
            RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
            StartWithWindowsCheckBox.IsChecked = rk?.GetValue("PHelper") != null;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(5);
            _timer.Tick += Timer_Tick;
            _timer.Start();
            CurrentModeText.Text = _currentMode.ToString();
            Timer_Tick(null, EventArgs.Empty);
        }

        private void ScanFolders_Click(object sender, RoutedEventArgs e)
        {
            var currentProcesses = _profiles.Select(p => p.ProcessName).ToList();
            
            ScannerDialog dialog = new ScannerDialog(currentProcesses);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                foreach (var item in dialog.SelectedExecutables)
                {
                    if (!_profiles.Any(p => p.ProcessName.Equals(item.FileName, System.StringComparison.OrdinalIgnoreCase)))
                    {
                        _profiles.Add(new AppProfile { ProcessName = item.FileName!, Mode = _defaultMode, FullPath = item.FullPath! });
                    }
                }

                foreach (var item in dialog.UnselectedExecutables)
                {
                    var profileToRemove = _profiles.FirstOrDefault(p => p.ProcessName.Equals(item.FileName, System.StringComparison.OrdinalIgnoreCase));
                    if (profileToRemove != null) _profiles.Remove(profileToRemove);
                }
                
                SaveConfigSilently();
            }
        }

        private void SetupTrayIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Visible = true
            };
            UpdateTrayIcon();
            
            _notifyIcon.DoubleClick += (s, e) =>
            {
                Show();
                WindowState = WindowState.Normal;
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Open", null, (s, e) => 
            {
                Show();
                WindowState = WindowState.Normal;
            });

            _pauseMenuItem = new ToolStripMenuItem("Pause Agent");
            _pauseMenuItem.DropDownItems.Add("1 Hour", null, (s, e) => PauseAgent(1));
            _pauseMenuItem.DropDownItems.Add("4 Hours", null, (s, e) => PauseAgent(4));
            _pauseMenuItem.DropDownItems.Add("8 Hours", null, (s, e) => PauseAgent(8));
            _pauseMenuItem.DropDownItems.Add("24 Hours", null, (s, e) => PauseAgent(24));
            _pauseMenuItem.DropDownItems.Add("Indefinitely", null, (s, e) => PauseAgent(0));
            contextMenu.Items.Add(_pauseMenuItem);

            _resumeMenuItem = new ToolStripMenuItem("Resume Agent");
            _resumeMenuItem.Click += (s, e) => ResumeAgent();
            _resumeMenuItem.Visible = false;
            contextMenu.Items.Add(_resumeMenuItem);

            contextMenu.Items.Add("Exit", null, (s, e) => 
            {
                _notifyIcon.Visible = false;
                Application.Current.Shutdown();
            });

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_isPaused)
            {
                if (_pauseUntil.HasValue && DateTime.Now >= _pauseUntil.Value)
                {
                    ResumeAgent();
                }
                else
                {
                    if (_pauseUntil.HasValue)
                    {
                        var remaining = _pauseUntil.Value - DateTime.Now;
                        PauseDurationComboBox.Text = $"{(int)remaining.TotalHours:D2}h {remaining.Minutes:D2}m";
                    }
                    return;
                }
            }

            var runningProcesses = Process.GetProcesses().Select(p => p.ProcessName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            
            TargetMode targetMode = _defaultMode;

            foreach (var profile in _profiles)
            {
                if (runningProcesses.Contains(profile.ProcessName))
                {
                    targetMode = profile.Mode;
                    if (targetMode == TargetMode.Turbo) break;
                }
            }

            if (_currentMode != targetMode)
            {
                _currentMode = targetMode;
                CurrentModeText.Text = _currentMode.ToString();
                GHelperHotkeys.SetMode(_currentMode);
                UpdateTrayIcon();
            }
        }

        private void DefaultModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DefaultModeComboBox.SelectedItem is TargetMode selectedMode)
            {
                _defaultMode = selectedMode;
                SaveConfigSilently();
            }
        }

        private void SaveConfig()
        {
            var config = new AppConfig
            {
                DefaultMode = _defaultMode,
                Profiles = _profiles.ToList()
            };
            ConfigManager.SaveConfig(config);
        }

        private void AddCurrentApp_Click(object sender, RoutedEventArgs e)
        {
            var runningApps = Process.GetProcesses()
                                     .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
                                     .Select(p => new ProcessInfo 
                                     { 
                                         ProcessName = p.ProcessName, 
                                         WindowTitle = p.MainWindowTitle 
                                     })
                                     .GroupBy(p => p.ProcessName)
                                     .Select(g => g.First())
                                     .OrderBy(p => p.ProcessName)
                                     .ToList();

            var dialog = new ProcessSelectionDialog(runningApps);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                string selectedProcess = dialog.SelectedProcess;
                if (!string.IsNullOrEmpty(selectedProcess) && !_profiles.Any(p => p.ProcessName.Equals(selectedProcess, StringComparison.OrdinalIgnoreCase)))
                {
                    _profiles.Add(new AppProfile { ProcessName = selectedProcess, Mode = TargetMode.Turbo });
                    SaveConfig();
                }
            }
        }

        private void RemoveProfile_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as System.Windows.Controls.Button)?.DataContext is AppProfile profile)
            {
                _profiles.Remove(profile);
                SaveConfig();
            }
        }

        private void SaveConfigSilently()
        {
            if (_profiles == null) return; 
            var config = ConfigManager.LoadConfig();
            if (config == null) return; 
            config.DefaultMode = _defaultMode;
            config.Profiles = _profiles.ToList();
            ConfigManager.SaveConfig(config);
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void StartWithWindowsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            string? appPath = Process.GetCurrentProcess().MainModule?.FileName;

            if (rk == null || appPath == null) return; 

            string appName = "PHelper";

            if (StartWithWindowsCheckBox.IsChecked == true)
            {
                rk.SetValue(appName, $"\"{appPath}\" -hidden");
            }
            else
            {
                rk.DeleteValue(appName, false);
            }
        }

        private int _lastSelectedPauseIndex = 4; // Default to 'Indefinitely'

        private void PauseAgent(double hours)
        {
            _lastSelectedPauseIndex = PauseDurationComboBox.SelectedIndex;
            _isPaused = true;
            if (hours > 0)
                _pauseUntil = DateTime.Now.AddHours(hours);
            else
                _pauseUntil = null;
            
            UpdatePauseUI();
            UpdateTrayIcon();
        }

        private void ResumeAgent()
        {
            _isPaused = false;
            _pauseUntil = null;
            
            UpdatePauseUI();
            UpdateTrayIcon();
            PauseDurationComboBox.SelectedIndex = _lastSelectedPauseIndex;
        }

        private void UpdatePauseUI()
        {
            if (_pauseMenuItem != null) _pauseMenuItem.Visible = !_isPaused;
            if (_resumeMenuItem != null) _resumeMenuItem.Visible = _isPaused;
            
            if (_isPaused)
            {
                PauseResumeButton.Content = "Resume";
                PauseDurationComboBox.IsEditable = true;
                PauseDurationComboBox.IsReadOnly = true;
                if (_pauseUntil.HasValue)
                {
                    var remaining = _pauseUntil.Value - DateTime.Now;
                    PauseDurationComboBox.Text = $"{(int)remaining.TotalHours:D2}h {remaining.Minutes:D2}m";
                }
                else
                {
                    PauseDurationComboBox.Text = "Indefinite";
                }
            }
            else
            {
                PauseResumeButton.Content = "Pause";
                PauseDurationComboBox.IsEditable = false;
                PauseDurationComboBox.IsReadOnly = false;
            }
        }

        private void PauseResumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isPaused)
            {
                ResumeAgent();
            }
            else
            {
                if (PauseDurationComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem item && 
                    item.Tag != null && double.TryParse(item.Tag.ToString(), out double hours))
                {
                    PauseAgent(hours);
                }
                else
                {
                    PauseAgent(0);
                }
            }
        }

        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.IsLoaded)
            {
                SaveConfigSilently();
            }
        }
    }
}