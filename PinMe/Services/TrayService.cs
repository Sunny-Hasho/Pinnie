using System;
using System.Windows.Forms;
using Pinnie.Interop;
using Microsoft.Win32.TaskScheduler;

namespace Pinnie.Services
{
    public class TrayService : IDisposable
    {
        private NotifyIcon? _notifyIcon;
        private TrayMenu? _trayMenu;
        
        public event EventHandler<HotkeySubMenu.HotkeyEventArgs>? HotkeyChanged;
        public event EventHandler? ExitRequested;
        public event EventHandler<bool>? ShowPetIconChanged;
        public event EventHandler<bool>? ShowBorderChanged;
        public event EventHandler<int>? BorderThicknessChanged;
        public event EventHandler<bool>? SoundEnabledChanged;
        public event EventHandler<int>? BorderRadiusChanged;
        public event EventHandler<System.Windows.Media.Brush>? BorderColorChanged;
        public event EventHandler<string>? PetIconChanged;
        public event EventHandler<int>? PetIconSizeChanged;
        public event EventHandler<string>? IconPositionChanged;
        
        public bool ShowPetIcon { get; set; } = true;
        public bool ShowBorder { get; set; } = true;
        public bool SoundEnabled { get; set; } = true;
        
        private const string AppName = "Pinnie";

        public void Initialize()
        {
            _trayMenu = new TrayMenu();
            bool isStartup = CheckStartup();
            _trayMenu.SetStates(ShowPetIcon, ShowBorder, isStartup, SoundEnabled);
            
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

            _trayMenu.SoundToggleClicked += (s, enabled) =>
            {
                SoundEnabled = enabled;
                SoundEnabledChanged?.Invoke(this, enabled);
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
                Text = AppName
            };
            
            _notifyIcon.MouseClick += NotifyIcon_MouseClick;
        }

        private bool CheckStartup()
        {
            // Query Task Scheduler COM API directly — no external processes.
            try
            {
                using var ts = new TaskService();
                return ts.GetTask(AppName) != null;
            }
            catch { return false; }
        }

        public void SetStartup(bool enable)
        {
            // Use Task Scheduler COM API:
            //   - No PowerShell, no schtasks.exe, no antivirus flags
            //   - LogonType = InteractiveToken  → only runs for the logged-in user
            //   - RunLevel  = Highest           → elevated (admin) with no UAC prompt
            try
            {
                using var ts = new TaskService();

                if (enable)
                {
                    var exePath = System.Environment.ProcessPath;
                    if (string.IsNullOrEmpty(exePath)) return;

                    TaskDefinition td = ts.NewTask();
                    td.RegistrationInfo.Description = "Starts Pinnie at user logon with administrator privileges.";

                    // Who runs it and at what privilege level
                    td.Principal.UserId    = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                    td.Principal.LogonType = TaskLogonType.InteractiveToken; // current interactive user only
                    td.Principal.RunLevel  = TaskRunLevel.Highest;           // elevated — no UAC popup

                    // When to run
                    td.Triggers.Add(new LogonTrigger());

                    // What to run
                    td.Actions.Add(new ExecAction(exePath));

                    // Misc settings
                    td.Settings.StopIfGoingOnBatteries      = false;
                    td.Settings.DisallowStartIfOnBatteries  = false;
                    td.Settings.ExecutionTimeLimit           = TimeSpan.Zero; // no timeout

                    ts.RootFolder.RegisterTaskDefinition(AppName, td);
                }
                else
                {
                    ts.RootFolder.DeleteTask(AppName, exceptionOnNotExists: false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting startup: {ex.Message}");
            }
        }

        private void NotifyIcon_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                if (_trayMenu != null && !_trayMenu.IsVisible)
                {
                    bool isStartup = CheckStartup();
                    _trayMenu.SetStates(ShowPetIcon, ShowBorder, isStartup, SoundEnabled);
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
