using System;
using System.Windows;
using System.Windows.Input;
using Pinnie.Interop;
using Pinnie.ViewModels;

namespace Pinnie
{
    public partial class SettingsWindow : Window
    {
        private readonly AppViewModel _viewModel;
        private uint _currentModifiers;
        private uint _currentKey;

        public SettingsWindow(AppViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
        }

        private void HotkeyBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
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

            // Convert WPF Key to Virtual Key
            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            
            _currentModifiers = modifiers;
            _currentKey = (uint)virtualKey;

            // Update Text
            string text = "";
            if ((modifiers & Win32.MOD_CONTROL) != 0) text += "Ctrl + ";
            if ((modifiers & Win32.MOD_ALT) != 0) text += "Alt + ";
            if ((modifiers & Win32.MOD_SHIFT) != 0) text += "Shift + ";
            if ((modifiers & Win32.MOD_WIN) != 0) text += "Win + ";
            text += key.ToString();

            HotkeyBox.Text = text;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_currentKey != 0)
            {
                _viewModel.UpdateHotkey(_currentModifiers, _currentKey);
                Close();
            }
            else
            {
                System.Windows.MessageBox.Show("Please select a valid hotkey first.");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
