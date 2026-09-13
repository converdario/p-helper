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
using System.Threading;
using System.Threading.Tasks;

namespace PHelper
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<AppProfile> _profiles;
        private TargetMode _defaultMode = TargetMode.Balanced;
        private NotifyIcon? _notifyIcon;
        private TargetMode _currentMode = TargetMode.Balanced;
        private IntPtr _currentIconHandle = IntPtr.Zero;
        private ToolStripMenuItem? _pauseMenuItem;
        private ToolStripMenuItem? _resumeMenuItem;
        private CancellationTokenSource? _saveCts;
        private AgentManager _agent;

        public MainWindow()
        {
            InitializeComponent();

            var config = ConfigManager.LoadConfig();
            _defaultMode = config.DefaultMode;
            _profiles = new ObservableCollection<AppProfile>(config.Profiles);
            ProfilesGrid.ItemsSource = _profiles;
            _profiles.CollectionChanged += (s, e) => RequestSaveConfig();

            DefaultModeComboBox.ItemsSource = Enum.GetValues(typeof(TargetMode));
            DefaultModeComboBox.SelectedItem = _defaultMode;

            // Inizializza l'Agent ma NON lo fa partire subito
            _agent = new AgentManager();
            _agent.ModeChanged += OnAgentModeChanged;
            _agent.PauseStateUpdated += UpdatePauseUI;
            _agent.SyncData(_profiles, _defaultMode);

            SetupTrayIcon();
            
            RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
            StartWithWindowsCheckBox.IsChecked = rk?.GetValue("PHelper") != null;

            UpdateCurrentModeUI();
            ProfilesGrid.SelectedItem = null;

            // 1. Intercetta l'avvio invisibile di Windows bloccando il rendering grafico
            string[] args = Environment.GetCommandLineArgs();
            if (args.Contains("-hidden", StringComparer.OrdinalIgnoreCase))
            {
                this.WindowState = WindowState.Minimized;
                this.Hide();
            }

            // 2. Ritarda la partenza del motore di scansione di 3 secondi
            _ = Task.Delay(3000).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    _agent.Start();
                    _ = _agent.PerformCheckAsync();
                });
            });
        }

        private void OnAgentModeChanged(TargetMode newMode)
        {
            _currentMode = newMode;
            UpdateCurrentModeUI();
            GHelperHotkeys.SetMode(_currentMode);
            UpdateTrayIcon();
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        private void UpdateTrayIcon()
        {
            System.Drawing.Color accentColor;

            // Legge lo stato di pausa direttamente dall'Agent
            bool isPaused = _agent?.IsPaused == true;

            if (isPaused)
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
            
            string statusText = isPaused ? "Paused" : _currentMode.ToString();
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

        private void UpdateCurrentModeUI()
        {
            CurrentModeText.Text = _currentMode.ToString();

            // Cambia il colore del testo in base alla modalità
            string hexColor = _currentMode switch
            {
                TargetMode.Silent => "#10E659",
                TargetMode.Balanced => "#00A8FF",
                TargetMode.Turbo => "#FF4343",
                _ => "#00A8FF"
            };
            
            CurrentModeText.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor));
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
                
                RequestSaveConfig();
            }
        }

        // Aggiungi questa per poter arrotondare la ContextMenuStrip
        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        public static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        // Funzione di supporto per generare le voci con il giusto padding
        private ToolStripMenuItem CreateMenuItem(string text, EventHandler? onClick = null, bool isTitle = false)
        {
            var item = new ToolStripMenuItem(text)
            {
                // Aumentato a 5 per creare più spaziatura verticale fra le voci
                Padding = new Padding(0, 5, 0, 5), 
                Enabled = !isTitle
            };
            if (onClick != null) item.Click += onClick;
            return item;
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
            contextMenu.Renderer = new DarkContextMenuRenderer();
            contextMenu.ShowImageMargin = true; 
            contextMenu.Font = new Font("Segoe UI", 9.5f);
            contextMenu.Padding = new Padding(0, 5, 0, 5); // Spazio in cima e in fondo al menù

            // Arrotonda gli angoli quando il menù si apre
            contextMenu.Opened += (s, e) => 
            {
                contextMenu.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, contextMenu.Width + 1, contextMenu.Height + 1, 12, 12));
            };

            // COSTRUZIONE MENU
            contextMenu.Items.Add(CreateMenuItem("Default CPU Profile", null, true));
            
            var silentItem = CreateMenuItem("Silet", (s, e) => SetManualMode(TargetMode.Silent));
            var balancedItem = CreateMenuItem("Balanced", (s, e) => SetManualMode(TargetMode.Balanced));
            var turboItem = CreateMenuItem("Turbo", (s, e) => SetManualMode(TargetMode.Turbo));
            
            contextMenu.Items.Add(silentItem);
            contextMenu.Items.Add(balancedItem);
            contextMenu.Items.Add(turboItem);

            // Aggiorna la spunta dinamicamente ogni volta che il menù si apre
            contextMenu.Opening += (s, e) =>
            {
                silentItem.Checked = _currentMode == TargetMode.Silent;
                balancedItem.Checked = _currentMode == TargetMode.Balanced;
                turboItem.Checked = _currentMode == TargetMode.Turbo;
            };

            contextMenu.Items.Add(new ToolStripSeparator());

            _pauseMenuItem = CreateMenuItem("Pause");
            _pauseMenuItem.DropDownItems.Add(CreateMenuItem("1 Hour", (s, e) => PauseAgent(1)));
            _pauseMenuItem.DropDownItems.Add(CreateMenuItem("4 Hours", (s, e) => PauseAgent(4)));
            _pauseMenuItem.DropDownItems.Add(CreateMenuItem("8 Hours", (s, e) => PauseAgent(8)));
            _pauseMenuItem.DropDownItems.Add(CreateMenuItem("24 Hours", (s, e) => PauseAgent(24)));
            _pauseMenuItem.DropDownItems.Add(CreateMenuItem("Indefinitely", (s, e) => PauseAgent(0)));
            
            // Applica il tema anche al sottomenù "Metti in Pausa"
            ((ToolStripDropDownMenu)_pauseMenuItem.DropDown).Renderer = new DarkContextMenuRenderer();
            ((ToolStripDropDownMenu)_pauseMenuItem.DropDown).ShowImageMargin = false; // Nessuna spunta necessaria qui
            
            contextMenu.Items.Add(_pauseMenuItem);

            _resumeMenuItem = CreateMenuItem("Resume", (s, e) => ResumeAgent());
            _resumeMenuItem.Visible = false;
            contextMenu.Items.Add(_resumeMenuItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            contextMenu.Items.Add(CreateMenuItem("Open P-Helper", (s, e) => 
            {
                Show();
                WindowState = WindowState.Normal;
            }));

            contextMenu.Items.Add(CreateMenuItem("Exit", (s, e) => 
            {
                _notifyIcon.Visible = false;
                Application.Current.Shutdown();
            }));

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void SetManualMode(TargetMode mode)
        {
            _defaultMode = mode;
            DefaultModeComboBox.SelectedItem = mode;
            RequestSaveConfig();
            
            // Aggiorna l'Agent e forza un check immediato
            if (_agent != null)
            {
                _agent.SyncData(_profiles, _defaultMode);
                _ = _agent.PerformCheckAsync();
            }
        }

        private void DefaultModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DefaultModeComboBox.SelectedItem is TargetMode selectedMode)
            {
                _defaultMode = selectedMode;
                RequestSaveConfig();
            }
        }

        private void AddCurrentApp_Click(object sender, RoutedEventArgs e)
        {
            var runningApps = new List<ProcessInfo>();
            
            // Recuperiamo i nomi dei processi già salvati per pre-selezionarli
            var existingProcessNames = _profiles.Select(p => p.ProcessName).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var p in Process.GetProcesses())
            {
                if (!string.IsNullOrEmpty(p.MainWindowTitle))
                {
                    string path = "";
                    try { path = p.MainModule?.FileName ?? ""; } catch { }

                    if (!runningApps.Any(x => x.ProcessName.Equals(p.ProcessName, StringComparison.OrdinalIgnoreCase)))
                    {
                        runningApps.Add(new ProcessInfo 
                        { 
                            ProcessName = p.ProcessName, 
                            WindowTitle = p.MainWindowTitle,
                            FullPath = path,
                            // Pre-imposta la spunta se il processo è già nei profili
                            IsSelected = existingProcessNames.Contains(p.ProcessName) 
                        });
                    }
                }
            }

            runningApps = runningApps.OrderBy(p => p.ProcessName).ToList();

            var dialog = new ProcessSelectionDialog(runningApps);
            dialog.Owner = this;
            
            if (dialog.ShowDialog() == true)
            {
                bool configChanged = false;

                // Aggiunge i processi selezionati che non sono ancora in lista
                foreach (var selected in dialog.SelectedProcesses)
                {
                    if (!_profiles.Any(p => p.ProcessName.Equals(selected.ProcessName, StringComparison.OrdinalIgnoreCase)))
                    {
                        _profiles.Add(new AppProfile { ProcessName = selected.ProcessName, FullPath = selected.FullPath, Mode = TargetMode.Turbo });
                        configChanged = true;
                    }
                }
                
                // Rimuove i processi deselezionati che erano in lista
                foreach (var unselected in dialog.UnselectedProcesses)
                {
                    var profileToRemove = _profiles.FirstOrDefault(p => p.ProcessName.Equals(unselected.ProcessName, StringComparison.OrdinalIgnoreCase));
                    if (profileToRemove != null)
                    {
                        _profiles.Remove(profileToRemove);
                        configChanged = true;
                    }
                }
                
                if (configChanged) RequestSaveConfig();
            }
        }

        private void RemoveProfile_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as System.Windows.Controls.Button)?.DataContext is AppProfile profile)
            {
                _profiles.Remove(profile);
                RequestSaveConfig();
            }
        }

        private async void RequestSaveConfig()
        {
            // Controllo di sicurezza: sincronizza i dati solo se l'agent è già stato caricato
            if (_agent != null && _profiles != null)
            {
                _agent.SyncData(_profiles, _defaultMode);
            }

            _saveCts?.Cancel();
            _saveCts = new CancellationTokenSource();
            var token = _saveCts.Token;

            try
            {
                await Task.Delay(1000, token);

                if (_profiles == null) return;
                
                var config = new AppConfig
                {
                    DefaultMode = _defaultMode,
                    Profiles = _profiles.ToList()
                };

                await Task.Run(() => ConfigManager.SaveConfig(config));
            }
            catch (TaskCanceledException)
            {
                // Ignorato correttamente
            }
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
            this.Hide();
        }

        private void StartWithWindowsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            string? appPath = Process.GetCurrentProcess().MainModule?.FileName;

            if (rk == null || appPath == null) return; 

            string appName = "P-Helper";

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
            _agent.Pause(hours);
            UpdateTrayIcon();
        }

        private void ResumeAgent()
        {
            _agent.Resume();
            UpdateTrayIcon();
            PauseDurationComboBox.SelectedIndex = _lastSelectedPauseIndex;
        }

        private void UpdatePauseUI()
        {
            if (_pauseMenuItem != null) _pauseMenuItem.Visible = !_agent.IsPaused;
            if (_resumeMenuItem != null) _resumeMenuItem.Visible = _agent.IsPaused;
            
            if (_agent.IsPaused)
            {
                PauseResumeButton.Content = "Resume";
                PauseDurationComboBox.IsEditable = true;
                PauseDurationComboBox.IsReadOnly = true;
                
                if (_agent.PauseUntil.HasValue)
                {
                    var remaining = _agent.PauseUntil.Value - DateTime.Now;
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
            if (_agent != null && _agent.IsPaused)
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
                RequestSaveConfig();
            }
        }

        private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ProfilesGrid.SelectedItem = null;
            System.Windows.Input.Keyboard.ClearFocus();
        }
    }
}