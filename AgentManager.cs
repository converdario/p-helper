using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace PHelper
{
    public class AgentManager
    {
        private DispatcherTimer _timer;
        private bool _isPaused = false;
        private DateTime? _pauseUntil = null;
        private TargetMode _currentMode = TargetMode.Balanced;
        
        private List<AppProfile> _profilesSnapshot = new();
        private TargetMode _defaultMode = TargetMode.Balanced;

        public event Action<TargetMode>? ModeChanged;
        public event Action? PauseStateUpdated;

        public AgentManager()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(5);
            _timer.Tick += async (s, e) => await PerformCheckAsync();
        }

        public void Start() => _timer.Start();

        public void SyncData(IEnumerable<AppProfile> profiles, TargetMode defaultMode)
        {
            _profilesSnapshot = profiles.ToList();
            _defaultMode = defaultMode;
        }

        public bool IsPaused => _isPaused;
        public DateTime? PauseUntil => _pauseUntil;

        public void Pause(double hours)
        {
            _isPaused = true;
            _pauseUntil = hours > 0 ? DateTime.Now.AddHours(hours) : null;
            PauseStateUpdated?.Invoke();
        }

        public void Resume()
        {
            _isPaused = false;
            _pauseUntil = null;
            PauseStateUpdated?.Invoke();
        }

        public async Task PerformCheckAsync()
        {
            if (_isPaused)
            {
                if (_pauseUntil.HasValue && DateTime.Now >= _pauseUntil.Value)
                {
                    Resume();
                }
                else
                {
                    PauseStateUpdated?.Invoke();
                    return;
                }
            }

            var profiles = _profilesSnapshot;
            var defaultMode = _defaultMode;

            TargetMode targetMode = await Task.Run(() =>
            {
                TargetMode mode = defaultMode;
                var runningProcesses = Process.GetProcesses()
                                              .Select(p => p.ProcessName)
                                              .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var profile in profiles)
                {
                    if (runningProcesses.Contains(profile.ProcessName))
                    {
                        mode = profile.Mode;
                        if (mode == TargetMode.Turbo) break; 
                    }
                }
                return mode;
            });

            if (_currentMode != targetMode)
            {
                _currentMode = targetMode;
                ModeChanged?.Invoke(_currentMode);
            }
        }
    }
}