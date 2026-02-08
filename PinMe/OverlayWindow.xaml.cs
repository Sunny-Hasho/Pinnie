using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Pinnie.Interop;

namespace Pinnie
{
    public partial class OverlayWindow : Window
    {
        public OverlayWindow()
        {
            InitializeComponent();
        }

        public IntPtr Handle { get; private set; }

        // Cache for optimization
        public double CachedDpiScale { get; set; } = 1.0;
        public Win32.RECT CachedFrameOffset { get; set; } // Left/Top/Right/Bottom diffs

        // For smoothing transitions (Maximize/Restore/Initial)
        public int SuppressionTicks { get; set; } = 0;

        protected override void OnSourceInitialized(System.EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.Handle = new WindowInteropHelper(this).Handle;
            
            // Set styles: Transparent (Click-through) | ToolWindow (Hide from Alt-Tab) | NoActivate (Never steal focus)
            int extendedStyle = Win32.GetWindowLongPtr(this.Handle, Win32.GWL_EXSTYLE).ToInt32();
            Win32.SetWindowLong(this.Handle, Win32.GWL_EXSTYLE, extendedStyle | (int)Win32.WS_EX_TRANSPARENT | (int)Win32.WS_EX_TOOLWINDOW | (int)Win32.WS_EX_NOACTIVATE);

            Win32.SetWindowPos(this.Handle, Win32.HWND_TOPMOST, 0, 0, 0, 0, Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_SHOWWINDOW);

            // Hook into the message loop to prevent Windows from clamping the window size (fixes 2K/4K monitor issues)
            HwndSource source = HwndSource.FromHwnd(this.Handle);
            source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Win32.WM_GETMINMAXINFO)
            {
                // Marshal the MINMAXINFO structure
                Win32.MINMAXINFO mmi = (Win32.MINMAXINFO)System.Runtime.InteropServices.Marshal.PtrToStructure(lParam, typeof(Win32.MINMAXINFO));

                // Adjust the maximizing size and position
                // Set to extremely large values to prevent clamping on high-res monitors
                mmi.ptMaxTrackSize.X = 30000;
                mmi.ptMaxTrackSize.Y = 30000;
                mmi.ptMaxSize.X = 30000;
                mmi.ptMaxSize.Y = 30000;
                
                // Copy back to unmanaged memory
                System.Runtime.InteropServices.Marshal.StructureToPtr(mmi, lParam, true);
            }

            return IntPtr.Zero;
        }

        public void SetPetVisible(bool visible)
        {
            PetIcon.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }
        public void SetBorderVisible(bool visible)
        {
            MainBorder.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public void SetBorderThickness(int thickness)
        {
            MainBorder.BorderThickness = new Thickness(thickness);
        }

        public void SetBorderCornerRadius(int radius)
        {
            MainBorder.CornerRadius = new CornerRadius(radius);
        }

        public void SetBorderBrush(System.Windows.Media.Brush brush)
        {
            MainBorder.BorderBrush = brush;
        }

        public void SetPetIconSource(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;

            try 
            {
                // Force a render reset (the "Kick")
                PetIcon.Visibility = Visibility.Collapsed;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad; 
                
                // Optimization: Decode strictly at the needed height (saves massive CPU during resize)
                // Use a standard height if not set yet.
                int decodeHeight = (int)(PetIcon.Height > 0 && !double.IsNaN(PetIcon.Height) ? PetIcon.Height : 50);
                bitmap.DecodePixelHeight = decodeHeight; 
                
                bitmap.EndInit();
                
                WpfAnimatedGif.ImageBehavior.SetAnimatedSource(PetIcon, bitmap);
                this.UpdateLayout(); 
                
                // Restore visibility
                PetIcon.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load icon: {ex.Message}");
                PetIcon.Visibility = Visibility.Visible;
            }
        }

        public void SetPetIconSize(int size)
        {
            PetIcon.Height = size;
            
            // Adjust Y offset based on size to eliminate gap for large icons
            // Small (30px): Y=8 (default offset)
            // Medium (50px): Y=8 (default offset)
            // Large (80px): Y=15 (move down to snap to tab bar)
            if (size >= 80)
            {
                PetIconTransform.Y = 15; // Large icon - move down more
            }
            else if (size >= 50)
            {
                PetIconTransform.Y = 8; // Medium icon - default offset
            }
            else
            {
                PetIconTransform.Y = 8; // Small icon - default offset
            }
        }

        public void SetPetIconPosition(string position)
        {
            switch (position)
            {
                case "Left":
                    PetIcon.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    PetIcon.Margin = new Thickness(4, 0, 0, 0);
                    break;
                case "Center":
                    PetIcon.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                    PetIcon.Margin = new Thickness(0, 0, 0, 0);
                    break;
                case "Right":
                    PetIcon.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                    PetIcon.Margin = new Thickness(0, 0, 4, 0);
                    break;
            }
        }
        public void SetHeaderHeight(double height)
        {
            if (HeaderRow.Height.Value != height)
            {
                HeaderRow.Height = new GridLength(height);
                this.UpdateLayout(); // Force layout update
            }
        }

        public double GetActualHeaderHeight()
        {
            // Get current DPI scale
            var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(this);
            // Return physical pixels (Logical Height * DPI Scale)
            // Use ActualHeight when available; fall back to configured Height value.
            double logicalHeight = HeaderRow.ActualHeight > 0 ? HeaderRow.ActualHeight : HeaderRow.Height.Value;
            return logicalHeight * dpi.DpiScaleY;
        }
    }
}
