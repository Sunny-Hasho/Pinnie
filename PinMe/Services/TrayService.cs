using System;
using System.Windows.Forms;
using PinWin.Interop;

namespace PinWin.Services
{
    public class TrayService : IDisposable
    {
        private NotifyIcon? _notifyIcon;
        private TrayMenu? _trayMenu;
        
        // Changed to use IntPtr for specific window
        public event EventHandler<IntPtr>? PinWindowRequested;
        public event EventHandler? UnpinRequested; // Keep generic unpin active for now or use same event?
        public event EventHandler? ExitRequested;
        public event EventHandler<bool>? ShowPetIconChanged;
        public event EventHandler<bool>? ShowBorderChanged;
        public event EventHandler<int>? BorderThicknessChanged;
        public event EventHandler<int>? BorderRadiusChanged;
        public event EventHandler<System.Windows.Media.Brush>? BorderColorChanged;
        public event EventHandler<string>? PetIconChanged;
        public event EventHandler<int>? PetIconSizeChanged;
        public event EventHandler<string>? IconPositionChanged;
        
        public bool ShowPetIcon { get; set; } = true;
        public bool ShowBorder { get; set; } = true;

        public void Initialize()
        {
            _trayMenu = new TrayMenu();
            _trayMenu.SetStates(ShowPetIcon, ShowBorder);
            
            // Wire up events
            _trayMenu.ShowPetIconToggleClicked += (s, enabled) =>
            {
                ShowPetIcon = enabled;
                ShowPetIconChanged?.Invoke(this, enabled);
            };
            
            _trayMenu.ShowBorderToggleClicked += (s, enabled) =>
            {
                ShowBorder = enabled;
                ShowBorderChanged?.Invoke(this, enabled);
            };
            
            // Old ChangeIconClicked removed - replaced by internal SubMenu logic
            
            _trayMenu.PetIconChanged += (s, path) =>
            {
                PetIconChanged?.Invoke(this, path);
            };
            
            _trayMenu.IconSizeChanged += (s, size) =>
            {
                PetIconSizeChanged?.Invoke(this, size);
            };
            
            _trayMenu.IconPositionChanged += (s, position) =>
            {
                IconPositionChanged?.Invoke(this, position);
            };
            
            _trayMenu.BorderThicknessChanged += (s, thickness) =>
            {
                BorderThicknessChanged?.Invoke(this, thickness);
            };
            
            _trayMenu.BorderRadiusChanged += (s, radius) =>
            {
                BorderRadiusChanged?.Invoke(this, radius);
            };
            
            _trayMenu.BorderColorChanged += (s, brush) =>
            {
                BorderColorChanged?.Invoke(this, brush);
            };
            
            _trayMenu.ExitClicked += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);

            _notifyIcon = new NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Information, // Placeholder
                Visible = true,
                Text = "PinWin"
            };
            
            _notifyIcon.MouseClick += NotifyIcon_MouseClick;
        }

        private void NotifyIcon_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                if (_trayMenu != null && !_trayMenu.IsVisible)
                {
                    _trayMenu.SetStates(ShowPetIcon, ShowBorder);
                    _trayMenu.ShowAtMouse();
                }
            }
        }

        public void Dispose()
        {
            _notifyIcon?.Dispose();
            _trayMenu?.Close();
        }
    }
}
