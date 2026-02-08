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

            // First run successfully
            // Show notification but continue running
            var notify = new NotificationWindow("Pinnie added Successfully!");
            notify.Show();
            
            // Standard startup logic continues (MainWindow which initializes the Tray)
            // Ensure the app doesn't close when the notification closes (since MainWindow is hidden)
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
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

