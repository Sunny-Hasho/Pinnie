using System;
using System.Collections.Generic;
using PinWin.Interop;

namespace PinWin.Services
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

            // Strategy: "ZIPPER CHAINING"
            // We interleave the Overlays into the Window Hierarchy to ensure correct Z-Order.
            
            // Desired Stack (Visual Top to Bottom):
            // Pin[0]
            // Overlay[0]
            // Pin[1]
            // Overlay[1]
            
            // Ownership Chain (Bottom Owns Top):
            // Overlay[1] (Bottom-most border) 
            //    -> Owns Pin[1] (Bottom Window)
            //        -> Owns Overlay[0] (Top Window Border)
            //            -> Owns Pin[0] (Top Window)
            
            // Wait, "Owner" is always BELOW "Child".
            // So if Pin1 Owns Pin0 -> Pin0 is ABOVE Pin1.
            
            // Correct Chain Logic:
            // A = Owner, B = Child. B is above A.
            
            // Chain:
            // Pin[Last] (Bottom Base)
            //   -> Owns Overlay[Last] (Border for Bottom)
            //       -> Owns Pin[Last-1] (Window Above)
            //           -> Owns Overlay[Last-1] (Border for Top)
            // ... and so on.

            // 1. Reset all links first to prevent cycles
            foreach (var pin in _pinnedStack)
            {
                Win32.SetWindowLong(pin, Win32.GWLP_HWNDPARENT, IntPtr.Zero);
                if (_overlayMap.TryGetValue(pin, out var ov))
                {
                    Win32.SetWindowLong(ov, Win32.GWLP_HWNDPARENT, IntPtr.Zero);
                }
            }

            // 2. Build the Zipper from Bottom Up (Last Index -> 0)
            
            // The "Current Base" is the Handle that will OWN the next item up the stack.
            // Start with the absolute bottom window.
            IntPtr currentBase = _pinnedStack[_pinnedStack.Count - 1]; // Bottom Pin
            
            // If bottom pin has an overlay, that overlay should sit on top of it.
            if (_overlayMap.TryGetValue(currentBase, out var bottomOverlay))
            {
                Win32.SetWindowLong(bottomOverlay, Win32.GWLP_HWNDPARENT, currentBase);
                currentBase = bottomOverlay; // Now the overlay is the "Top" of the bottom pile
            }

            // Now iterate upwards
            for (int i = _pinnedStack.Count - 2; i >= 0; i--)
            {
                IntPtr nextUpPin = _pinnedStack[i];
                
                // Link: Bottom Pile -> Owns -> Next Window Up
                Win32.SetWindowLong(nextUpPin, Win32.GWLP_HWNDPARENT, currentBase);
                currentBase = nextUpPin;

                // Link: Next Window Up -> Owns -> Its Overlay
                if (_overlayMap.TryGetValue(nextUpPin, out var overlay))
                {
                    Win32.SetWindowLong(overlay, Win32.GWLP_HWNDPARENT, currentBase);
                    currentBase = overlay;
                }
            }
            
            // 3. Set Everything to TopMost to float above normal windows
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

        private void UnpinWindow(IntPtr hWnd)
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
    }
}
