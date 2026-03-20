using System;
using System.Collections.Generic;
using Pinnie.Interop;

namespace Pinnie.Services
{
    public class WindowPinService
    {
        private List<IntPtr> _pinnedStack = new List<IntPtr>();

        // Returns true if pinned (TopMost), false if unpinned (NotTopMost)
        // Returns true if pinned (TopMost), false if unpinned (NotTopMost)
        public bool TogglePin(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return false;

            // Use internal state first. "Spamming" protection.
            if (_pinnedStack.Contains(hWnd))
            {
                UnpinWindow(hWnd);
                return false; // Result is unpinned
            }
            else
            {
                PinWindow(hWnd);
                return true; // Result is pinned
            }
        }

        public bool IsPinned(IntPtr hWnd)
        {
            return _pinnedStack.Contains(hWnd);
        }

        public bool IsWindowTopMost(IntPtr hWnd)
        {
            IntPtr exStyle = Win32.GetWindowLongPtr(hWnd, Win32.GWL_EXSTYLE);
            bool isTop = (exStyle.ToInt64() & Win32.WS_EX_TOPMOST) == Win32.WS_EX_TOPMOST;
            Logger.Log($"IsWindowTopMost: HWND {hWnd}, Style {exStyle.ToInt64():X}, Result: {isTop}");
            return isTop;
        }

        public WindowPinService()
        {
            // No more Timers or Hooks.
            // We rely on Native Kernel Ownership for 100% stability.
        }

        private Dictionary<IntPtr, IntPtr> _overlayMap = new Dictionary<IntPtr, IntPtr>();

        public void RegisterOverlay(IntPtr pin, IntPtr overlay)
        {
            if (overlay == IntPtr.Zero) return;
            _overlayMap[pin] = overlay;
            EnforceStackOrder();
        }

        public void UnregisterOverlay(IntPtr pin)
        {
            if (_overlayMap.ContainsKey(pin))
            {
                _overlayMap.Remove(pin);
            }
        }

        private void EnforceStackOrder()
        {
            // Validate handles first
            _pinnedStack.RemoveAll(h => !Win32.IsWindowNotEmpty(h));

            if (_pinnedStack.Count == 0) return;

            // 1. Reset all links first to prevent cycles and cross-monitor leaks
            foreach (var pin in _pinnedStack)
            {
                SetOwnerIfChanged(pin, IntPtr.Zero);
                if (_overlayMap.TryGetValue(pin, out var ov))
                {
                    SetOwnerIfChanged(ov, IntPtr.Zero);
                }
            }

            // 2. Group windows by their current monitor
            // This prevents "Zipper Chaining" from creating cross-monitor ownership which causes glitches.
            var monitorGroups = new Dictionary<IntPtr, List<IntPtr>>();
            foreach (var pin in _pinnedStack)
            {
                IntPtr hMonitor = Win32.MonitorFromWindow(pin, Win32.MONITOR_DEFAULTTONEAREST);
                if (!monitorGroups.ContainsKey(hMonitor))
                    monitorGroups[hMonitor] = new List<IntPtr>();
                monitorGroups[hMonitor].Add(pin);
            }

            // 3. Build the Zipper per monitor from Bottom Up
            foreach (var group in monitorGroups.Values)
            {
                if (group.Count == 0) continue;

                // Monitor-local stack top
                IntPtr currentBase = group[group.Count - 1]; // Bottom Pin for this monitor

                if (_overlayMap.TryGetValue(currentBase, out var bottomOverlay))
                {
                    Win32.SetWindowLong(bottomOverlay, Win32.GWLP_HWNDPARENT, currentBase);
                    currentBase = bottomOverlay;
                }

                for (int i = group.Count - 2; i >= 0; i--)
                {
                    IntPtr nextUpPin = group[i];
                    Win32.SetWindowLong(nextUpPin, Win32.GWLP_HWNDPARENT, currentBase);
                    currentBase = nextUpPin;

                    if (_overlayMap.TryGetValue(nextUpPin, out var overlay))
                    {
                        Win32.SetWindowLong(overlay, Win32.GWLP_HWNDPARENT, currentBase);
                        currentBase = overlay;
                    }
                }
            }
            
            // 4. Set Everything to TopMost to float above normal windows
            foreach (var hwnd in _pinnedStack)
            {
                 Win32.SetWindowPos(hwnd, Win32.HWND_TOPMOST, 0, 0, 0, 0, Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE);
                 if (_overlayMap.TryGetValue(hwnd, out var ov))
                 {
                     Win32.SetWindowPos(ov, Win32.HWND_TOPMOST, 0, 0, 0, 0, Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE);
                 }
            }
        }

        private void PinWindow(IntPtr hWnd)
        {
            if (!_pinnedStack.Contains(hWnd))
            {
                _pinnedStack.Add(hWnd);
            }
            
            EnforceStackOrder();
        }

        public void UnpinWindow(IntPtr hWnd)
        {
            if (_pinnedStack.Contains(hWnd))
            {
                // Break its links before removing!
                // If we don't, removing it might close the others or leave them orphaned.
                Win32.SetWindowLong(hWnd, Win32.GWLP_HWNDPARENT, IntPtr.Zero);
                _pinnedStack.Remove(hWnd);
            }

            // Unpin from TopMost
            Win32.SetWindowPos(hWnd, Win32.HWND_NOTOPMOST, 0, 0, 0, 0, Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE);
            Logger.Log($"UnpinWindow: HWND {hWnd}");
            
            // Re-chain remaining
            EnforceStackOrder();
        }
        private void SetOwnerIfChanged(IntPtr hwnd, IntPtr newOwner)
        {
            IntPtr currentOwner = Win32.GetWindowLongPtr(hwnd, Win32.GWLP_HWNDPARENT);
            if (currentOwner != newOwner)
            {
                IntPtr result = Win32.SetWindowLong(hwnd, Win32.GWLP_HWNDPARENT, newOwner);
                if (result == IntPtr.Zero && System.Runtime.InteropServices.Marshal.GetLastWin32Error() != 0)
                {
                    int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                    Logger.Log($"SetOwnerIfChanged Failed: HWND {hwnd}, Owner {newOwner}, Error {error}");
                    
                    if (error == 5) // Access Denied
                    {
                        Logger.Log("ACCESS DENIED: Pinnie might need Administrator privileges to manage this window.");
                    }
                }
            }
        }
    }
}
