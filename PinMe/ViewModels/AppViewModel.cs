using System;
using System.Windows;
using Pinnie.Interop;
using Pinnie.Services;

namespace Pinnie.ViewModels
{
    public class AppViewModel : IDisposable
    {
        private readonly WindowPinService _pinService;
        private readonly HotkeyService _hotkeyService;
        private readonly TrayService _trayService;
        private readonly OverlayService _overlayService;
        private readonly SoundService _soundService;
        private readonly SettingsService _settingsService;
        private IntPtr _mainWindowHandle;
        private IntPtr _foregroundHook = IntPtr.Zero;
        private Win32.WinEventDelegate _foregroundHookDelegate; // Keep ref to prevent GC
        private IntPtr _currentForegroundWindow = IntPtr.Zero;
        private IntPtr _pendingForegroundWindow = IntPtr.Zero;
        private System.Windows.Threading.DispatcherTimer _focusDebounceTimer;

        public AppViewModel()
        {
            _settingsService = new SettingsService(); // Load settings first
            _pinService = new WindowPinService();
            _hotkeyService = new HotkeyService();
            _trayService = new TrayService();
            _overlayService = new OverlayService();
            _soundService = new SoundService();

            // Initialize Focus Debounce Timer
            _focusDebounceTimer = new System.Windows.Threading.DispatcherTimer();
            _focusDebounceTimer.Interval = TimeSpan.FromMilliseconds(100); // 100ms delay to accept focus
            _focusDebounceTimer.Tick += FocusDebounceTimer_Tick;

            // Initialize Foreground Tracking Hook
            _foregroundHookDelegate = new Win32.WinEventDelegate(ForegroundEventProc);
            _foregroundHook = Win32.SetWinEventHook(
                Win32.EVENT_SYSTEM_FOREGROUND, 
                Win32.EVENT_SYSTEM_FOREGROUND, 
                IntPtr.Zero, 
                _foregroundHookDelegate, 
                0, 0, 
                Win32.WINEVENT_OUTOFCONTEXT);

            _trayService.Initialize();

            _trayService.ExitRequested += (s, e) => 
            {
                _trayService.SetStartup(false);
                System.Windows.Application.Current.Shutdown();
            };
            
            _trayService.HotkeyChanged += (s, e) =>
            {
                UpdateHotkey(e.Modifiers, e.Key);
            };

            _trayService.PinWindowRequested += (s, hwnd) => TogglePinState(hwnd); 
            _trayService.ShowPetIconChanged += (s, enabled) => _overlayService.SetPetIconState(enabled);
            _trayService.ShowBorderChanged += (s, enabled) => _overlayService.SetBorderState(enabled);
            _trayService.BorderThicknessChanged += (s, thickness) => _overlayService.SetBorderThickness(thickness);
            _trayService.BorderRadiusChanged += (s, radius) => _overlayService.SetCornerRadius(radius);
            _trayService.BorderColorChanged += (s, color) => _overlayService.SetBorderColor(color);
            _trayService.PetIconChanged += (s, path) => _overlayService.SetPetIcon(path);
            _trayService.PetIconSizeChanged += (s, size) => _overlayService.SetPetIconSize(size);
            _trayService.IconPositionChanged += (s, position) => _overlayService.SetPetIconPosition(position);

            _hotkeyService.HotkeyPressed += (s, e) => ToggleActiveWindowPin();
        }

        private void ForegroundEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (eventType == Win32.EVENT_SYSTEM_FOREGROUND && hwnd != IntPtr.Zero)
            {
                // Normalize logic immediately
                IntPtr rootWindow = Win32.GetAncestor(hwnd, Win32.GA_ROOTOWNER);
                if (rootWindow != IntPtr.Zero)
                {
                    hwnd = rootWindow;
                }
                
                // Start Debounce: Don't commit yet, wait 100ms to see if it settles
                _pendingForegroundWindow = hwnd;
                _focusDebounceTimer.Stop();
                _focusDebounceTimer.Start();
            }
        }

        private void FocusDebounceTimer_Tick(object sender, EventArgs e)
        {
            _focusDebounceTimer.Stop();
            
            if (_pendingForegroundWindow != IntPtr.Zero && _pendingForegroundWindow != _currentForegroundWindow)
            {
                Logger.Log($"ForegroundTracker: Stable Focus Changed to {_pendingForegroundWindow}");
                _currentForegroundWindow = _pendingForegroundWindow;
            }
        }

        public void Initialize(IntPtr windowHandle)
        {
            _mainWindowHandle = windowHandle;
            
            uint modifiers = _settingsService.CurrentSettings.HotkeyModifiers;
            uint key = _settingsService.CurrentSettings.HotkeyKey;

            Logger.Log($"Initializing Hotkey: Modifiers={modifiers}, Key={key:X}");

            if (!_hotkeyService.Register(_mainWindowHandle, modifiers, key))
            {
                System.Windows.MessageBox.Show("Failed to register hotkey. It might be in use.", "PinWin Error");
            }
            
            // Update Tray Display
            _trayService.UpdateHotkeyDisplay(modifiers, key);
            
            // Seed initial state
            _currentForegroundWindow = Win32.GetForegroundWindow();
        }

        public void UpdateHotkey(uint modifiers, uint key)
        {
            _hotkeyService.Unregister(_mainWindowHandle);
            
            if (_hotkeyService.Register(_mainWindowHandle, modifiers, key))
            {
                _settingsService.CurrentSettings.HotkeyModifiers = modifiers;
                _settingsService.CurrentSettings.HotkeyKey = key;
                _settingsService.Save();
                
                _trayService.UpdateHotkeyDisplay(modifiers, key);
                
                Logger.Log($"Hotkey updated to Modifiers={modifiers}, Key={key:X}");
            }
            else
            {
                // Revert? Or just show error
                System.Windows.MessageBox.Show("Failed to register new hotkey. Keeping old one.", "PinWin Error");
                // Try re-registering old one
                 _hotkeyService.Register(_mainWindowHandle, _settingsService.CurrentSettings.HotkeyModifiers, _settingsService.CurrentSettings.HotkeyKey);
            }
        }

        public void ProcessMessage(int msg, IntPtr wParam)
        {
            _hotkeyService.ProcessMessage(msg, wParam);
        }

        private void ToggleActiveWindowPin()
        {
             // NEW STRATEGY: Target the window under the mouse cursor.
             // This solves "Active Window" ambiguity by relying on user's pointing intent.
             
             Win32.POINT cursor;
             IntPtr hwnd = IntPtr.Zero;

             if (Win32.GetCursorPos(out cursor))
             {
                 hwnd = Win32.WindowFromPoint(cursor);
                 Logger.Log($"ToggleActiveWindowPin: Cursor at {cursor.X},{cursor.Y} -> HWND {hwnd}");
             }
             
             if (hwnd == IntPtr.Zero)
             {
                 // Fallback to Foreground if mouse fails
                 hwnd = Win32.GetForegroundWindow();
                 Logger.Log($"ToggleActiveWindowPin: Cursor failed, falling back to Foreground HWND {hwnd}");
             }

             // Normalize logic: Get root owner to handle child windows/controls
             IntPtr rootWindow = Win32.GetAncestor(hwnd, Win32.GA_ROOTOWNER);
             if (rootWindow != IntPtr.Zero)
             {
                 Logger.Log($"ToggleActiveWindowPin: Normalized {hwnd} to root owner {rootWindow}");
                 hwnd = rootWindow;
             }
             
             // Check if the user has focused (or system thinks focused) the overlay
             if (_overlayService.TryGetTargetFromOverlay(hwnd, out var realTarget))
             {
                 Logger.Log($"ToggleActiveWindowPin: Redirecting from Overlay {hwnd} to Target {realTarget}");
                 hwnd = realTarget;
             }

             IntPtr targetWindow = hwnd;

             // Prioritize: If the target window is pinned, toggle it.
             if (_pinService.IsPinned(hwnd))
             {
                 Logger.Log($"ToggleActiveWindowPin: Window {hwnd} is PINNED - will toggle (unpin) it");
                 targetWindow = hwnd;
             }
             else
             {
                 Logger.Log($"ToggleActiveWindowPin: Window {hwnd} is NOT PINNED - will pin it");
                 targetWindow = hwnd;
             }

             TogglePinState(targetWindow);
        }

        private void TogglePinState(IntPtr handle)
        {
            Logger.Log($"AppViewModel: Toggle request for {handle}");
            bool isPinned = _pinService.TogglePin(handle);
            
            if (isPinned)
            {
                Logger.Log("AppViewModel: Adding overlay");
                IntPtr overlayHandle = _overlayService.AddOverlay(handle);
                _pinService.RegisterOverlay(handle, overlayHandle);
                _soundService.PlayPinSound();
            }
            else
            {
                Logger.Log("AppViewModel: Removing overlay");
                _overlayService.RemoveOverlay(handle);
                _pinService.UnregisterOverlay(handle);
                _soundService.PlayUnpinSound();
            }
        }

        private void PinCurrentWindow()
        {
            ToggleActiveWindowPin(); 
        }

        public void Dispose()
        {
            _hotkeyService.Unregister(_mainWindowHandle);
            if (_foregroundHook != IntPtr.Zero)
            {
                Win32.UnhookWinEvent(_foregroundHook);
                _foregroundHook = IntPtr.Zero;
            }
            _trayService.Dispose();
            _overlayService.Dispose();
        }
    }
}
