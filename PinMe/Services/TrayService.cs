using System;
using System.Windows.Forms;
using Pinnie.Interop;
using Microsoft.Win32;

namespace Pinnie.Services
{
    public class TrayService : IDisposable
    {
        private NotifyIcon? _notifyIcon;
        private TrayMenu? _trayMenu;
        
        public event EventHandler<IntPtr>? PinWindowRequested;
        public event EventHandler? UnpinRequested; 
        public event EventHandler<HotkeySubMenu.HotkeyEventArgs>? HotkeyChanged;
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
        
        private const string AppName = "Pinnie";

        public void Initialize()
        {
            _trayMenu = new TrayMenu();
            bool isStartup = CheckStartup();
            _trayMenu.SetStates(ShowPetIcon, ShowBorder, isStartup);
            
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
            
            _trayMenu.StartupToggleClicked += (s, enabled) => SetStartup(enabled);
            
            _trayMenu.PetIconChanged += (s, path) => PetIconChanged?.Invoke(this, path);
            _trayMenu.IconSizeChanged += (s, size) => PetIconSizeChanged?.Invoke(this, size);
            _trayMenu.IconPositionChanged += (s, position) => IconPositionChanged?.Invoke(this, position);
            _trayMenu.BorderThicknessChanged += (s, thickness) => BorderThicknessChanged?.Invoke(this, thickness);
            _trayMenu.BorderRadiusChanged += (s, radius) => BorderRadiusChanged?.Invoke(this, radius);
            _trayMenu.BorderColorChanged += (s, brush) => BorderColorChanged?.Invoke(this, brush);
            _trayMenu.HotkeyChanged += (s, e) => HotkeyChanged?.Invoke(this, e);
            _trayMenu.ExitClicked += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);

            System.Drawing.Icon appIcon = System.Drawing.SystemIcons.Application;
            try
            {
                // Try to load from file first (most reliable for dev/transient builds)
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_icon.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    appIcon = new System.Drawing.Icon(iconPath);
                }
                else
                {
                    // Fallback to EXE extraction
                    var exeIcon = System.Drawing.Icon.ExtractAssociatedIcon(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "");
                    if (exeIcon != null) appIcon = exeIcon;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load icon: {ex.Message}");
            }

            _notifyIcon = new NotifyIcon
            {
                Icon = appIcon,
                Visible = true,
                Text = "PinMe"
            };
            
            _notifyIcon.MouseClick += NotifyIcon_MouseClick;
        }

        private bool CheckStartup()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    return key != null && key.GetValue(AppName) != null;
                }
            }
            catch { return false; }
        }

        public void SetStartup(bool enable)
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key == null) return;

                    if (enable)
                    {
                        var path = System.Environment.ProcessPath;
                        if (!string.IsNullOrEmpty(path))
                        {
                            // Ensure path is quoted if it contains spaces
                            if (!path.StartsWith("\"") && path.Contains(" "))
                            {
                                path = $"\"{path}\"";
                            }
                            key.SetValue(AppName, path);
                        }
                    }
                    else
                    {
                        key.DeleteValue(AppName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error settings startup: {ex.Message}");
            }
        }

        private void NotifyIcon_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                if (_trayMenu != null && !_trayMenu.IsVisible)
                {
                    bool isStartup = CheckStartup();
                    _trayMenu.SetStates(ShowPetIcon, ShowBorder, isStartup);
                    _trayMenu.ShowAtMouse();
                }
            }
        }

        public void UpdateHotkeyDisplay(uint modifiers, uint key)
        {
            _trayMenu?.UpdateHotkeyDisplay(modifiers, key);
        }

        public void Dispose()
        {
            _notifyIcon?.Dispose();
            _trayMenu?.Close();
        }
    }
}
