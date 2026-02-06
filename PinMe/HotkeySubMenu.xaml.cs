using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Pinnie.Interop;

namespace Pinnie
{
    public partial class HotkeySubMenu : Window
    {
        public event EventHandler<HotkeyEventArgs>? HotkeyChanged;
        private bool _isRecording = false;

        public class HotkeyEventArgs : EventArgs
        {
            public uint Modifiers { get; set; }
            public uint Key { get; set; }
        }

        public HotkeySubMenu()
        {
            InitializeComponent();
        }

        public void SetCurrentHotkey(uint modifiers, uint key)
        {
            TxtCurrentHotkey.Text = $"Current: {FormatHotkeyString(modifiers, key)}";
        }

        private string FormatHotkeyString(uint modifiers, uint keyId)
        {
            string text = "";
            if ((modifiers & Win32.MOD_CONTROL) != 0) text += "Ctrl + ";
            if ((modifiers & Win32.MOD_ALT) != 0) text += "Alt + ";
            if ((modifiers & Win32.MOD_SHIFT) != 0) text += "Shift + ";
            if ((modifiers & Win32.MOD_WIN) != 0) text += "Win + ";
            
            try 
            {
                text += KeyInterop.KeyFromVirtualKey((int)keyId).ToString();
            }
            catch 
            {
                text += ((char)keyId).ToString();
            }
            
            return text;
        }

        private void BtnRecord_Click(object sender, RoutedEventArgs e)
        {
            _isRecording = true;
            BtnRecord.Content = "Press keys now...";
            BtnRecord.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 240, 240)); // Light red tint
            
            // Focus self to capture keys
            this.Activate();
            this.Focus();
        }

        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (!_isRecording) return;
            
            e.Handled = true;

            // Get modifiers
            uint modifiers = 0;
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) modifiers |= Win32.MOD_CONTROL;
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) modifiers |= Win32.MOD_ALT;
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) modifiers |= Win32.MOD_SHIFT;
            if ((Keyboard.Modifiers & ModifierKeys.Windows) == ModifierKeys.Windows) modifiers |= Win32.MOD_WIN;

            // Handle Key
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);

            // Ignore modifier keys themselves
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            // Valid key pressed
            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            
            // Fire Event
            HotkeyChanged?.Invoke(this, new HotkeyEventArgs { Modifiers = modifiers, Key = (uint)virtualKey });
            
            // Update UI
            SetCurrentHotkey(modifiers, (uint)virtualKey);
            
            // Reset State
            _isRecording = false;
            BtnRecord.Content = "Change Hotkey...";
            BtnRecord.Background = System.Windows.Media.Brushes.Transparent;
            
            this.Hide();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            // Default: Ctrl + Win + T
            uint mod = Win32.MOD_CONTROL | Win32.MOD_WIN;
            uint key = 0x54; // T
            
            HotkeyChanged?.Invoke(this, new HotkeyEventArgs { Modifiers = mod, Key = key });
            SetCurrentHotkey(mod, key);
            this.Hide();
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

           // Adjust if off screen
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
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (_isRecording) return; // Don't close if we are just switching focus for recording
            this.Hide();
        }
        
        public new void Hide()
        {
             _isRecording = false;
             BtnRecord.Content = "Change Hotkey...";
             BtnRecord.Background = System.Windows.Media.Brushes.Transparent;
             base.Hide();
        }
    }
}
