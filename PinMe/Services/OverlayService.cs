using System;
using System.Collections.Generic;
using System.Windows.Threading;
using PinWin.Interop;
using System.Linq;
using System.Runtime.InteropServices;

namespace PinWin.Services
{
    public class OverlayService : IDisposable
    {
        private Dictionary<IntPtr, OverlayWindow> _overlays = new Dictionary<IntPtr, OverlayWindow>();
        private DispatcherTimer _trackingTimer;
        private Win32.WinEventDelegate _winEventDelegate;
        private IntPtr _hookHandle = IntPtr.Zero;

        public OverlayService()
        {
            _trackingTimer = new DispatcherTimer();
            _trackingTimer.Interval = TimeSpan.FromMilliseconds(50); // Relaxed timer as Hook handles moves
            _trackingTimer.Tick += TrackingTimer_Tick;
            _trackingTimer.Start();

            _winEventDelegate = new Win32.WinEventDelegate(WinEventProc);
            // Hook global LOCATIONCHANGE (Move/Size)
            _hookHandle = Win32.SetWinEventHook(
                Win32.EVENT_OBJECT_LOCATIONCHANGE, 
                Win32.EVENT_OBJECT_LOCATIONCHANGE, 
                IntPtr.Zero, 
                _winEventDelegate, 
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
            overlay.Show();
            _overlays.Add(targetHwnd, overlay);
            Logger.Log($"OverlayService: Added overlay for {targetHwnd}");

            // Note: We do NOT set Owner here anymore.
            // WindowPinService handles the complex "Zipper" chaining.
            
            // Immediate update
            UpdateOverlayState(targetHwnd, overlay);
            UpdateOverlayPositionFast(targetHwnd, overlay);

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
            // ToList to allow modification (removal) during iteration if needed, though we mainly remove on "close" detection
            var keys = _overlays.Keys.ToList();
            foreach (var hwnd in keys)
            {
                if (_overlays.TryGetValue(hwnd, out var overlay))
                {
                    // Slow tick: Check state (Maximization, Visibility, Cloaking)
                    UpdateOverlayState(hwnd, overlay);
                    // Also update position as fallback
                    UpdateOverlayPositionFast(hwnd, overlay);
                }
            }
        }

        public bool IsPetIconEnabled { get; set; } = true;
        public bool IsBorderEnabled { get; set; } = true;
        public int CurrentBorderThickness { get; set; } = 4;
        public int CurrentCornerRadius { get; set; } = 8; // Defaulting to 8px as requested
        public System.Windows.Media.Brush CurrentBorderBrush { get; set; } = System.Windows.Media.Brushes.White;
        public string? CurrentPetIconPath { get; set; }
        public int CurrentPetIconSize { get; set; } = 50; // New default: 50px (Medium)

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
            foreach (var overlay in _overlays.Values)
            {
                overlay.SetPetIconSource(path);
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
                // Force position update with correct HWND
                UpdateOverlayPositionFast(hwnd, overlay);
            }
        }

        private void UpdateOverlayState(IntPtr hwnd, OverlayWindow overlay)
        {
             // 1. Check Basic Visibility
            if (!Win32.IsWindowVisible(hwnd))
            {
                overlay.Visibility = System.Windows.Visibility.Hidden;
                return;
            }

            // 2. Check "Cloaked" State (UWP/Virtual Desktop)
            int cloakedVal;
            Win32.DwmGetWindowAttribute(hwnd, Win32.DWMWA_CLOAKED, out cloakedVal, sizeof(int));
            if (cloakedVal != 0)
            {
                overlay.Visibility = System.Windows.Visibility.Hidden;
                return;
            }

            // Check Maximized State for Pet Icon
            bool isMaximized = false;
            var placement = new Win32.WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(typeof(Win32.WINDOWPLACEMENT));
            if (Win32.GetWindowPlacement(hwnd, ref placement))
            {
                if (placement.showCmd == Win32.SW_SHOWMAXIMIZED)
                    isMaximized = true;
            }

            // Show pet if NOT maximized AND Enabled globally
            bool showPet = !isMaximized && IsPetIconEnabled;
            overlay.SetPetVisible(showPet);

            if (overlay.Visibility != System.Windows.Visibility.Visible)
            {
                overlay.Visibility = System.Windows.Visibility.Visible;
            }

            // --- CACHING LOGIC START ---
            // 1. DPI
            int dpi = Win32.GetDpiForWindow(hwnd);
            if (dpi == 0) dpi = 96;
            overlay.CachedDpiScale = dpi / 96.0;

            // 2. Frame Offset (Visual Bounds vs Window Rect)
            // We need both rectangles to calculate the diff.
            Win32.RECT visualRect;
            int result = Win32.DwmGetWindowAttribute(hwnd, Win32.DWMWA_EXTENDED_FRAME_BOUNDS, out visualRect, Marshal.SizeOf(typeof(Win32.RECT)));
            
            // If DWM fails, we default to 0 offset (assume match)
            if (result == 0)
            {
                Win32.RECT winRect;
                if (Win32.GetWindowRect(hwnd, out winRect))
                {
                    var offset = new Win32.RECT();
                    offset.Left = visualRect.Left - winRect.Left;
                    offset.Top = visualRect.Top - winRect.Top;
                    offset.Right = visualRect.Right - winRect.Right; 
                    offset.Bottom = visualRect.Bottom - winRect.Bottom;
                    overlay.CachedFrameOffset = offset;
                }
            }
            // --- CACHING LOGIC END ---
        }

        private void UpdateOverlayPositionFast(IntPtr hwnd, OverlayWindow overlay)
        {
            if (hwnd == IntPtr.Zero) return; // Safety check

            // FASTEST PATH: Use GetWindowRect (User32) + Cached Offset
            // This avoids DwmGetWindowAttribute (IPC) and GetDpi in the hot path.
            
            Win32.RECT winRect;
            if (!Win32.GetWindowRect(hwnd, out winRect))
            {
                return;
            }

            // Apply Cached Offsets to get Visual Bounds
            var offset = overlay.CachedFrameOffset;
            int left = winRect.Left + offset.Left;
            int top = winRect.Top + offset.Top;
            int right = winRect.Right + offset.Right;   // Note: Cached Right is diff of Right coordinates
            int bottom = winRect.Bottom + offset.Bottom;

            int width = right - left;
            int height = bottom - top;

            if (width <= 0 || height <= 0)
            {
                return;
            }

            int finalTop = top;
            int finalHeight = height;
            
            // Use Cached DPI
            double scale = overlay.CachedDpiScale;
            
            // Calculate offset based on CURRENT visibility state
            if (overlay.PetIcon.Visibility == System.Windows.Visibility.Visible)
            {
                int petOffset = (int)Math.Ceiling(CurrentPetIconSize * scale);
                finalTop -= petOffset;
                finalHeight += petOffset;
            }

            // Direct Win32 positioning
            Win32.SetWindowPos(overlay.Handle, IntPtr.Zero, 
                left, finalTop, width, finalHeight, 
                Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE | Win32.SWP_SHOWWINDOW);
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            // Only care about Window objects (0). 
            // Filtering out child objects (-4 client, etc.) reduces spam significantly during resize/layout.
            if (idObject != Win32.OBJID_WINDOW) return;

            // Simplified check: just check if we track this HWND.
            if (_overlays.TryGetValue(hwnd, out var overlay))
            {
                // Fast path: Only update position
                UpdateOverlayPositionFast(hwnd, overlay);
            }
        }

        public void Dispose()
        {
            _trackingTimer.Stop();
            if (_hookHandle != IntPtr.Zero)
            {
                Win32.UnhookWinEvent(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
            foreach (var overlay in _overlays.Values)
            {
                overlay.Close();
            }
            _overlays.Clear();
        }
    }
}
