using System;
using System.Linq;
using System.Threading;
using System.Windows;

namespace PHelper
{
    public partial class App : System.Windows.Application
    {
        private static Mutex? _mutex;
        private static EventWaitHandle? _eventWaitHandle;
        
        private const string UniqueAppName = "GHelperProfileAgent_Unique_ID_12345"; 

        protected override void OnStartup(StartupEventArgs e)
        {
            _mutex = new Mutex(true, UniqueAppName + "_Mutex", out bool isNewInstance);

            if (!isNewInstance)
            {
                try
                {
                    var waitHandle = EventWaitHandle.OpenExisting(UniqueAppName + "_Event");
                    waitHandle.Set(); 
                }
                catch { }

                System.Windows.Application.Current.Shutdown();
                return;
            }


            _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, UniqueAppName + "_Event");
            
            ThreadPool.QueueUserWorkItem((state) =>
            {
                while (_eventWaitHandle.WaitOne())
                {
                    Current.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        var mainWindow = Current.MainWindow;
                        if (mainWindow != null)
                        {
                            mainWindow.Show();
                            
                            if (mainWindow.WindowState == WindowState.Minimized)
                            {
                                mainWindow.WindowState = WindowState.Normal;
                            }
                            
                            mainWindow.Activate();
                            mainWindow.Topmost = true;  
                            mainWindow.Topmost = false; 
                            mainWindow.Focus();
                        }
                    }));
                }
            });

            base.OnStartup(e);

            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            MainWindow mainWindow = new MainWindow();
            
            this.MainWindow = mainWindow; 
            
            if (!e.Args.Contains("-hidden"))
            {
                mainWindow.Show();
            }
        }
    }
}