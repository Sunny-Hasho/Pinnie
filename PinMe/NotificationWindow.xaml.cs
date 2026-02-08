using System;
using System.Windows;
using System.Windows.Threading;

namespace Pinnie
{
    public partial class NotificationWindow : Window
    {
        private DispatcherTimer _closeTimer;

        public NotificationWindow(string message)
        {
            InitializeComponent();
            MessageText.Text = message;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            _closeTimer = new DispatcherTimer();
            _closeTimer.Interval = TimeSpan.FromSeconds(3); // Show for 3 seconds
            _closeTimer.Tick += CloseTimer_Tick;
            _closeTimer.Start();
        }

        private void CloseTimer_Tick(object? sender, EventArgs e)
        {
            _closeTimer.Stop();
            
            // Fade out animation could be added here, but for now just close
            var anim = new System.Windows.Media.Animation.DoubleAnimation(0, TimeSpan.FromMilliseconds(300));
            anim.Completed += (s, _) => this.Close();
            this.BeginAnimation(UIElement.OpacityProperty, anim);
        }
    }
}
