using System.Configuration;
using System.Data;
using System.Windows;

namespace Pinnie;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
    public partial class App : System.Windows.Application
    {
        private System.Threading.Mutex? _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "PinnieSingleInstanceMutex";
            bool createdNew;

            _mutex = new System.Threading.Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                // App is already running
                ShowNotificationAndExit("Pinnie is already there", true);
                return;
            }

            base.OnStartup(e);

            // Show "added successfully" ONLY on the very first ever launch.
            // Subsequent startups (including auto-start at boot) are silent.
            if (IsFirstEverLaunch())
            {
                var notify = new NotificationWindow("Pinnie added Successfully!");
                notify.Show();
            }

            // Ensure the app doesn't close when the notification closes (MainWindow is hidden)
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        /// <summary>
        /// Returns true the first time the app ever launches, then marks it as seen.
        /// Uses a simple registry key so it persists across reboots.
        /// </summary>
        private static bool IsFirstEverLaunch()
        {
            const string regKey   = @"Software\Pinnie";
            const string regValue = "HasLaunchedBefore";
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(regKey, writable: true)
                             ?? Microsoft.Win32.Registry.CurrentUser.CreateSubKey(regKey);

                if (key?.GetValue(regValue) != null)
                    return false;          // already seen — stay silent

                key?.SetValue(regValue, 1); // mark as seen for all future launches
                return true;
            }
            catch { return false; }         // if registry fails, stay silent to be safe
        }

        private void ShowNotificationAndExit(string message, bool isError)
        {
            var notify = new NotificationWindow(message);
            notify.Show();
            
            // Wait for notification to close before shutting down? 
            // Or just let it run for a bit.
            // Since we're in OnStartup, if we return, the app might exit if no window is open.
            // But we want to show the specific message then die.
            
            // Force message loop to run so window shows
            var frame = new System.Windows.Threading.DispatcherFrame();
            notify.Closed += (s, e) => frame.Continue = false;
            System.Windows.Threading.Dispatcher.PushFrame(frame);
            
            Environment.Exit(0);
        }
    }

