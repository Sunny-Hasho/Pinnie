using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Pinnie
{
    public partial class SubMenu : Window
    {
        public event EventHandler<string>? ItemClicked;
        private System.Windows.Threading.DispatcherTimer? _closeTimer;

        public SubMenu()
        {
            InitializeComponent();
            _closeTimer = new System.Windows.Threading.DispatcherTimer();
            _closeTimer.Interval = TimeSpan.FromMilliseconds(50);
            _closeTimer.Tick += CloseTimer_Tick;
        }

        private void CloseTimer_Tick(object? sender, EventArgs e)
        {
            if (!this.IsVisible) return;

            bool mouseButtonDown = (Pinnie.Interop.Win32.GetAsyncKeyState(Pinnie.Interop.Win32.VK_LBUTTON) & 0x8000) != 0 ||
                                 (Pinnie.Interop.Win32.GetAsyncKeyState(Pinnie.Interop.Win32.VK_RBUTTON) & 0x8000) != 0;

            if (mouseButtonDown && !this.IsMouseOver)
            {
                this.Hide();
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            IntPtr exStyle = Pinnie.Interop.Win32.GetWindowLongPtr(hwnd, Pinnie.Interop.Win32.GWL_EXSTYLE);
            Pinnie.Interop.Win32.SetWindowLong(hwnd, Pinnie.Interop.Win32.GWL_EXSTYLE,
                new IntPtr(exStyle.ToInt64() | (long)Pinnie.Interop.Win32.WS_EX_NOACTIVATE | (long)Pinnie.Interop.Win32.WS_EX_TOOLWINDOW));
        }

        public void AddMenuItem(string text, string value)
        {
            var button = new System.Windows.Controls.Button
            {
                Content = text,
                Style = (Style)this.Resources["SubMenuItemStyle"]
            };

            button.Click += (s, e) =>
            {
                ItemClicked?.Invoke(this, value);
                this.Hide();
            };

            MenuItems.Children.Add(button);
        }

        public void ClearItems()
        {
            MenuItems.Children.Clear();
        }

        public void ShowNextTo(FrameworkElement element, Window parentWindow)
        {
            // Get the position of the element relative to screen
            System.Windows.Point elementPos = element.PointToScreen(new System.Windows.Point(0, 0));
            
            DpiScale dpi = VisualTreeHelper.GetDpi(this);
            double scaleX = dpi.DpiScaleX;
            double scaleY = dpi.DpiScaleY;

            double wpfX = elementPos.X / scaleX;
            double wpfY = elementPos.Y / scaleY;

            // Position to the right of the parent menu
            this.Left = wpfX + element.ActualWidth + 5;
            this.Top = wpfY;

            // Adjust if off screen
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            if (this.Left + this.Width > screenWidth)
            {
                this.Left = wpfX - this.Width - 5; // Show on left instead
            }

            if (this.Top + this.ActualHeight > screenHeight)
            {
                this.Top = screenHeight - this.ActualHeight - 10;
            }

            this.Show();
            _closeTimer?.Start();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.Hide();
        }

        public new void Hide()
        {
            if (!this.IsVisible) return;
            _closeTimer?.Stop();
            base.Hide();
        }
    }
}
