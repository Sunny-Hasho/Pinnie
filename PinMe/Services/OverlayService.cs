using System;
using System.Collections.Generic;
using System.Windows.Threading;
using Pinnie.Interop;
using System.Linq;
using System.Runtime.InteropServices;

namespace Pinnie.Services
{
    public class OverlayService : IDisposable
    {
        private Dictionary<IntPtr, OverlayWindow> _overlays = new Dictionary<IntPtr, OverlayWindow>();
        private Dictionary<IntPtr, bool> _isAnimating = new Dictionary<IntPtr, bool>();
        private DispatcherTimer _trackingTimer;
        private IntPtr _animationHook;
        private Win32.WinEventDelegate? _animationHookDelegate;

        public OverlayService()
        {
            // Set priority to Send (highest) to ensure tracking logic isn't delayed by layout/rendering
            _trackingTimer = new DispatcherTimer(System.Windows.Threading.DispatcherPriority.Send);
            _trackingTimer.Interval = TimeSpan.FromMilliseconds(10); // High-speed polling (User Request)
            _trackingTimer.Tick += TrackingTimer_Tick;
            _trackingTimer.Start();

            // Set default icon to Capybara (Pack URI)
            CurrentPetIconPath = "pack://application:,,,/Assets/capy.gif";

            // Hook animation events for seamless transitions
            _animationHookDelegate = new Win32.WinEventDelegate(AnimationEventProc);
            _animationHook = Win32.SetWinEventHook(
                Win32.EVENT_SYSTEM_MOVESIZESTART,
                Win32.EVENT_SYSTEM_MOVESIZEEND,
                IntPtr.Zero,
                _animationHookDelegate,
                0,
                0,
                Win32.WINEVENT_OUTOFCONTEXT);
        }



        public IntPtr AddOverlay(IntPtr targetHwnd)
        {
            if (_overlays.ContainsKey(targetHwnd))
            {
                Logger.Log($"OverlayService: Already tracking {targetHwnd}");
                return _overlays[targetHwnd].Handle;
            }

            var overlay = new OverlayWindow();
            overlay.SuppressionTicks = 5; // Suppress for 50ms to allow first position calc
            overlay.Show();
            _overlays.Add(targetHwnd, overlay);
            Logger.Log($"OverlayService: Added overlay for {targetHwnd}");

            // Note: We do NOT set Owner here anymore.
            // WindowPinService handles the complex "Zipper" chaining.
            
            // Immediate update
            // Immediate update
            UpdateOverlayPosition(targetHwnd, overlay);

            // Apply global settings
            // Pet visibility is handled in UpdateOverlayPosition loop
            overlay.SetBorderVisible(IsBorderEnabled);
            overlay.SetBorderThickness(CurrentBorderThickness);
            overlay.SetBorderThickness(CurrentBorderThickness);
            overlay.SetBorderCornerRadius(CurrentCornerRadius);
            overlay.SetBorderThickness(CurrentBorderThickness);
            overlay.SetBorderCornerRadius(CurrentCornerRadius);
            overlay.SetBorderBrush(CurrentBorderBrush);
            if (!string.IsNullOrEmpty(CurrentPetIconPath))
            {
                overlay.SetPetIconSource(CurrentPetIconPath);
            }
            overlay.SetPetIconSize(CurrentPetIconSize);
            
            return overlay.Handle;
        }

        public void RemoveOverlay(IntPtr targetHwnd)
        {
            if (_overlays.ContainsKey(targetHwnd))
            {
                var overlay = _overlays[targetHwnd];
                overlay.Close();
                _overlays.Remove(targetHwnd);
                _isAnimating.Remove(targetHwnd);
                Logger.Log($"OverlayService: Removed overlay for {targetHwnd}");
            }
            else 
            {
                Logger.Log($"OverlayService: Request to remove known {targetHwnd} but not found in dict");
            }
        }

        public bool TryGetTargetFromOverlay(IntPtr overlayHandle, out IntPtr targetHandle)
        {
            targetHandle = IntPtr.Zero;
            foreach (var kvp in _overlays)
            {
                if (kvp.Value.Handle == overlayHandle)
                {
                    targetHandle = kvp.Key;
                    return true;
                }
            }
            return false;
        }

        private void TrackingTimer_Tick(object sender, EventArgs e)
        {
             var keys = _overlays.Keys.ToList();
             foreach (var hwnd in keys)
             {
                 if (_overlays.TryGetValue(hwnd, out var overlay))
                 {
                     UpdateOverlayPosition(hwnd, overlay);
                 }
             }
        }

        public bool IsPetIconEnabled { get; set; } = true;
        public bool IsBorderEnabled { get; set; } = true;
        public int CurrentBorderThickness { get; set; } = 2; // Default: Small (2px)
        public int CurrentCornerRadius { get; set; } = 8; // Defaulting to 8px as requested
        public System.Windows.Media.Brush CurrentBorderBrush { get; set; } = System.Windows.Media.Brushes.White;
        public string? CurrentPetIconPath { get; set; }
        public int CurrentPetIconSize { get; set; } = 50; // New default: 50px (Medium)
        public string CurrentPetIconPosition { get; set; } = "Center"; // Default: Center

        public void SetPetIconState(bool enabled)
        {
            IsPetIconEnabled = enabled;
            // Iterate not needed for Pet as Tick handles it via UpdateOverlayPosition -> SetPetVisible
            // Actually UpdateOverlayPosition is called every tick so checking Flag there is enough.
        }

        public void SetBorderState(bool enabled)
        {
            IsBorderEnabled = enabled;
            foreach (var overlay in _overlays.Values)
            {
                overlay.SetBorderVisible(enabled);
            }
        }

        public void SetBorderThickness(int thickness)
        {
            CurrentBorderThickness = thickness;
            foreach (var overlay in _overlays.Values)
            {
                overlay.SetBorderThickness(thickness);
            }
        }

        public void SetCornerRadius(int radius)
        {
            CurrentCornerRadius = radius;
            foreach (var overlay in _overlays.Values)
            {
                overlay.SetBorderCornerRadius(radius);
            }
        }

        public void SetBorderColor(System.Windows.Media.Brush color)
        {
            CurrentBorderBrush = color;
            foreach (var overlay in _overlays.Values)
            {
                overlay.SetBorderBrush(color);
            }
        }

        public void SetPetIcon(string path)
        {
            CurrentPetIconPath = path;
            foreach (var kvp in _overlays)
            {
                var hwnd = kvp.Key;
                var overlay = kvp.Value;
                overlay.SetPetIconSource(path);
                UpdateOverlayPosition(hwnd, overlay);
            }
        }

        public void SetPetIconSize(int size)
        {
            CurrentPetIconSize = size;
            // Iterate Copy of keys or just KVP directly to avoid modification issues (though set shouldn't modify dict)
            foreach (var kvp in _overlays)
            {
                var hwnd = kvp.Key;
                var overlay = kvp.Value;
                overlay.SetPetIconSize(size);
                UpdateOverlayPosition(hwnd, overlay);
            }
        }

        public void SetPetIconPosition(string position)
        {
            CurrentPetIconPosition = position;
            foreach (var kvp in _overlays)
            {
                var hwnd = kvp.Key;
                var overlay = kvp.Value;
                overlay.SetPetIconPosition(position);
                UpdateOverlayPosition(hwnd, overlay);
            }
        }

        private void UpdateOverlayPosition(IntPtr hwnd, OverlayWindow overlay)
        {
            // Check if window is currently animating
            bool isAnimating = _isAnimating.TryGetValue(hwnd, out bool animFlag) && animFlag;

            Win32.RECT rect;
            // Native DWM call - fast enough for 10ms polling
            int result = Win32.DwmGetWindowAttribute(hwnd, Win32.DWMWA_EXTENDED_FRAME_BOUNDS, out rect, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Win32.RECT)));
            
            if (result != 0)
            {
                 // Fallback to GetWindowRect to verify if window is closed
                 Win32.RECT temp;
                 if (!Win32.GetWindowRect(hwnd, out temp))
                 {
                    RemoveOverlay(hwnd); // Window closed
                    return;
                 }
                 return; // Just skip update
            }

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            if (width <= 0 || height <= 0)
            {
                overlay.Visibility = System.Windows.Visibility.Hidden;
                return;
            }

            // 1. Basic Visibility
            if (!Win32.IsWindowVisible(hwnd))
            {
                overlay.Visibility = System.Windows.Visibility.Hidden;
                return;
            }

            // 2. Cloaked
            int cloakedVal;
            Win32.DwmGetWindowAttribute(hwnd, Win32.DWMWA_CLOAKED, out cloakedVal, sizeof(int));
            if (cloakedVal != 0)
            {
                overlay.Visibility = System.Windows.Visibility.Hidden;
                return;
            }

            // 3. Maximized Check (for Pet)
            bool isMaximized = false;
            var placement = new Win32.WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(typeof(Win32.WINDOWPLACEMENT));
            if (Win32.GetWindowPlacement(hwnd, ref placement))
            {
                if (placement.showCmd == Win32.SW_SHOWMAXIMIZED)
                    isMaximized = true;
            }

            // State-change detection: Hide ONLY during maximize/restore transitions
            bool lastKnownMax = (overlay.Tag is bool b) && b;
            if (isAnimating && lastKnownMax != isMaximized)
            {
                // State is changing during animation - hide until animation ends
                overlay.Tag = isMaximized;
                overlay.Visibility = System.Windows.Visibility.Hidden;
                return;
            }
            // If state changed but animation ended, add settling delay
            else if (!isAnimating && lastKnownMax != isMaximized)
            {
                overlay.Tag = isMaximized;
                overlay.SuppressionTicks = 5; // 50ms settling time after state transition
            }
            // Update state even if not animating
            overlay.Tag = isMaximized;

            // Settling delay after animation ends
            if (overlay.SuppressionTicks > 0)
            {
                overlay.SuppressionTicks--;
                overlay.Visibility = System.Windows.Visibility.Hidden;
                return;
            }

            // Show pet if NOT maximized AND Enabled
            bool showPet = !isMaximized && IsPetIconEnabled;
            overlay.SetPetVisible(showPet);

            // Ensure visibility after suppression ends
            if (overlay.Visibility != System.Windows.Visibility.Visible)
            {
                overlay.Visibility = System.Windows.Visibility.Visible;
            }

            int finalTop = rect.Top;
            int finalHeight = height;

            // 4. Update Header Height based on state
            // Decoupled from "Show Pet" flag to prevent glitches when toggling icon.
            // If Maximized, collapse header to 0.
            // If Restored, ALWAYS use 150px header for stability.
            bool useFixedHeader = !isMaximized;
            
            if (useFixedHeader)
            {
                overlay.SetHeaderHeight(150);
            }
            else
            {
                overlay.SetHeaderHeight(0);
            }

            // ROBUST OFFSET CALCULATION
            // We need to offset the window up by the physical height of the header row.
            
            int petOffset = 0;
            try 
            {
                // Try to get exact rendered height from WPF
                double actualHeight = overlay.GetActualHeaderHeight();
                if (actualHeight > 0)
                {
                    // Use ceiling to avoid undersizing due to fractional DPI scaling.
                    petOffset = (int)Math.Ceiling(actualHeight);
                }
            }
            catch 
            {
                // Ignore errors
            }

            // Fallback: If we expect a Fixed Header but got 0/bad value
            if (useFixedHeader && petOffset <= 0)
            {
                 int dpi = Win32.GetDpiForWindow(hwnd);
                 if (dpi == 0) dpi = 96;
                 double scale = dpi / 96.0;
                 petOffset = (int)Math.Ceiling(150 * scale);
                 
                 // Safety clamp for restored windows
                 if (petOffset < 150) petOffset = 150;
            }
            
            finalTop -= petOffset;
            finalHeight += petOffset;

            // showPet passed to SetPetVisible handles the icon visibility itself
            // relying on SetHeaderHeight(0) handles the structural space
            {
                // Pet visibility is already set via SetPetVisible above
            }

            // Position
            Win32.SetWindowPos(overlay.Handle, IntPtr.Zero, 
                rect.Left, finalTop, width, finalHeight, 
                Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE | Win32.SWP_SHOWWINDOW);
        }

        private void AnimationEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            // Only process window-level events
            if (idObject != Win32.OBJID_WINDOW) return;

            // Only track windows we're monitoring
            if (!_overlays.ContainsKey(hwnd)) return;

            if (eventType == Win32.EVENT_SYSTEM_MOVESIZESTART)
            {
                // Mark as animating, but don't hide yet - let update loop decide
                _isAnimating[hwnd] = true;
            }
            else if (eventType == Win32.EVENT_SYSTEM_MOVESIZEEND)
            {
                // Animation ended - clear flag (settling delay added only if state changed)
                _isAnimating[hwnd] = false;
            }
        }

        public void Dispose()
        {
            _trackingTimer.Stop();
            
            if (_animationHook != IntPtr.Zero)
            {
                Win32.UnhookWinEvent(_animationHook);
                _animationHook = IntPtr.Zero;
            }
            
            foreach (var overlay in _overlays.Values)
            {
                overlay.Close();
            }
            _overlays.Clear();
            _isAnimating.Clear();
        }
    }
}
