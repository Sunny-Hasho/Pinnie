using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PinWin
{
    public partial class ColorPickerPopup : Window
    {
        public event EventHandler<System.Windows.Media.Color>? ColorSelected;
        private System.Windows.Threading.DispatcherTimer? _closeTimer;

        private static readonly System.Windows.Media.Color[] Colors = new System.Windows.Media.Color[]
        {
            // Row 1 - Reds/Pinks
            System.Windows.Media.Color.FromRgb(255, 179, 186), System.Windows.Media.Color.FromRgb(255, 128, 128), 
            System.Windows.Media.Color.FromRgb(255, 0, 0), System.Windows.Media.Color.FromRgb(200, 0, 0),
            System.Windows.Media.Color.FromRgb(139, 0, 0), System.Windows.Media.Color.FromRgb(255, 105, 180),
            System.Windows.Media.Color.FromRgb(255, 20, 147), System.Windows.Media.Color.FromRgb(199, 21, 133),
            
            // Row 2 - Oranges/Yellows
            System.Windows.Media.Color.FromRgb(255, 218, 185), System.Windows.Media.Color.FromRgb(255, 165, 0),
            System.Windows.Media.Color.FromRgb(255, 140, 0), System.Windows.Media.Color.FromRgb(255, 215, 0),
            System.Windows.Media.Color.FromRgb(255, 255, 0), System.Windows.Media.Color.FromRgb(255, 255, 224),
            System.Windows.Media.Color.FromRgb(255, 250, 205), System.Windows.Media.Color.FromRgb(255, 228, 181),
            
            // Row 3 - Greens
            System.Windows.Media.Color.FromRgb(144, 238, 144), System.Windows.Media.Color.FromRgb(0, 255, 0),
            System.Windows.Media.Color.FromRgb(0, 200, 0), System.Windows.Media.Color.FromRgb(0, 128, 0),
            System.Windows.Media.Color.FromRgb(34, 139, 34), System.Windows.Media.Color.FromRgb(0, 100, 0),
            System.Windows.Media.Color.FromRgb(154, 205, 50), System.Windows.Media.Color.FromRgb(124, 252, 0),
            
            // Row 4 - Cyans/Teals
            System.Windows.Media.Color.FromRgb(175, 238, 238), System.Windows.Media.Color.FromRgb(0, 255, 255),
            System.Windows.Media.Color.FromRgb(0, 206, 209), System.Windows.Media.Color.FromRgb(64, 224, 208),
            System.Windows.Media.Color.FromRgb(72, 209, 204), System.Windows.Media.Color.FromRgb(0, 128, 128),
            System.Windows.Media.Color.FromRgb(32, 178, 170), System.Windows.Media.Color.FromRgb(95, 158, 160),
            
            // Row 5 - Blues
            System.Windows.Media.Color.FromRgb(173, 216, 230), System.Windows.Media.Color.FromRgb(135, 206, 250),
            System.Windows.Media.Color.FromRgb(0, 191, 255), System.Windows.Media.Color.FromRgb(30, 144, 255),
            System.Windows.Media.Color.FromRgb(0, 0, 255), System.Windows.Media.Color.FromRgb(0, 0, 205),
            System.Windows.Media.Color.FromRgb(0, 0, 139), System.Windows.Media.Color.FromRgb(25, 25, 112),
            
            // Row 6 - Purples
            System.Windows.Media.Color.FromRgb(221, 160, 221), System.Windows.Media.Color.FromRgb(238, 130, 238),
            System.Windows.Media.Color.FromRgb(255, 0, 255), System.Windows.Media.Color.FromRgb(218, 112, 214),
            System.Windows.Media.Color.FromRgb(186, 85, 211), System.Windows.Media.Color.FromRgb(147, 112, 219),
            System.Windows.Media.Color.FromRgb(138, 43, 226), System.Windows.Media.Color.FromRgb(128, 0, 128),
            
            // Row 7 - Browns/Grays
            System.Windows.Media.Color.FromRgb(245, 245, 220), System.Windows.Media.Color.FromRgb(222, 184, 135),
            System.Windows.Media.Color.FromRgb(210, 180, 140), System.Windows.Media.Color.FromRgb(188, 143, 143),
            System.Windows.Media.Color.FromRgb(165, 42, 42), System.Windows.Media.Color.FromRgb(139, 69, 19),
            System.Windows.Media.Color.FromRgb(160, 82, 45), System.Windows.Media.Color.FromRgb(205, 133, 63),
            
            // Row 8 - Grays/Black/White
            System.Windows.Media.Color.FromRgb(255, 255, 255), System.Windows.Media.Color.FromRgb(220, 220, 220),
            System.Windows.Media.Color.FromRgb(192, 192, 192), System.Windows.Media.Color.FromRgb(169, 169, 169),
            System.Windows.Media.Color.FromRgb(128, 128, 128), System.Windows.Media.Color.FromRgb(105, 105, 105),
            System.Windows.Media.Color.FromRgb(64, 64, 64), System.Windows.Media.Color.FromRgb(0, 0, 0)
        };

        public ColorPickerPopup()
        {
            InitializeComponent();
            InitializeColors();
            
            _closeTimer = new System.Windows.Threading.DispatcherTimer();
            _closeTimer.Interval = TimeSpan.FromMilliseconds(50);
            _closeTimer.Tick += CloseTimer_Tick;
        }

        private void InitializeColors()
        {
            foreach (var color in Colors)
            {
                var border = new Border
                {
                    Background = new SolidColorBrush(color),
                    Style = (Style)this.Resources["ColorButtonStyle"],
                    Tag = color
                };

                border.MouseLeftButtonDown += ColorBorder_Click;
                ColorGrid.Children.Add(border);
            }
        }

        private void ColorBorder_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is System.Windows.Media.Color color)
            {
                ColorSelected?.Invoke(this, color);
                this.Hide();
            }
        }

        private void CloseTimer_Tick(object? sender, EventArgs e)
        {
            if (!this.IsVisible) return;

            bool mouseButtonDown = (PinWin.Interop.Win32.GetAsyncKeyState(PinWin.Interop.Win32.VK_LBUTTON) & 0x8000) != 0;

            if (mouseButtonDown && !this.IsMouseOver)
            {
                this.Hide();
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            IntPtr exStyle = PinWin.Interop.Win32.GetWindowLongPtr(hwnd, PinWin.Interop.Win32.GWL_EXSTYLE);
            PinWin.Interop.Win32.SetWindowLong(hwnd, PinWin.Interop.Win32.GWL_EXSTYLE,
                new IntPtr(exStyle.ToInt64() | (long)PinWin.Interop.Win32.WS_EX_NOACTIVATE | (long)PinWin.Interop.Win32.WS_EX_TOOLWINDOW));
        }

        public void ShowNextTo(FrameworkElement element, Window parentWindow)
        {
            System.Windows.Point elementPos = element.PointToScreen(new System.Windows.Point(0, 0));

            DpiScale dpi = VisualTreeHelper.GetDpi(this);
            double scaleX = dpi.DpiScaleX;
            double scaleY = dpi.DpiScaleY;

            double wpfX = elementPos.X / scaleX;
            double wpfY = elementPos.Y / scaleY;

            this.Left = wpfX + element.ActualWidth + 5;
            this.Top = wpfY;

            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            if (this.Left + this.Width > screenWidth)
            {
                this.Left = wpfX - this.Width - 5;
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
