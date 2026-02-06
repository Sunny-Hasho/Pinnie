using System;
using System.Runtime.InteropServices;
using Pinnie.Interop;

namespace Pinnie.Services
{
    public class HotkeyService
    {
        private const int ID_HOTKEY = 9000;
        public event EventHandler HotkeyPressed;

        public bool Register(IntPtr hWnd, uint modifiers, uint key)
        {
            return Win32.RegisterHotKey(hWnd, ID_HOTKEY, modifiers, key);
        }

        public void Unregister(IntPtr hWnd)
        {
            Win32.UnregisterHotKey(hWnd, ID_HOTKEY);
        }

        public void ProcessMessage(int msg, IntPtr wParam)
        {
            if (msg == Win32.WM_HOTKEY && wParam.ToInt32() == ID_HOTKEY)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
