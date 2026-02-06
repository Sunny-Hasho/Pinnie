using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Forms;

namespace Pinnie
{
    public partial class TrayMenu : Window
    {
        public event EventHandler<bool>? ShowPetIconToggleClicked;
        public event EventHandler<bool>? ShowBorderToggleClicked;
        public event EventHandler<bool>? StartupToggleClicked;
        public event EventHandler? ChangeIconClicked;
        public event EventHandler<int>? IconSizeChanged;
        public event EventHandler<string>? IconPositionChanged;
        public event EventHandler<int>? BorderThicknessChanged;
        public event EventHandler<int>? BorderRadiusChanged;
        public event EventHandler? BorderColorClicked;
        public event EventHandler<System.Windows.Media.Brush>? BorderColorChanged;
        public event EventHandler<HotkeySubMenu.HotkeyEventArgs>? HotkeyChanged;
        public event EventHandler? ExitClicked;

        public event EventHandler<string>? PetIconChanged;

        private bool _showPetIcon = true;
        private bool _showBorder = true;
        private bool _runOnStartup = false;
        private SubMenu? _iconSizeSubMenu;
        private SubMenu? _iconPositionSubMenu;
        private SubMenu? _changeIconSubMenu; 
        private SubMenu? _borderThicknessSubMenu;
        private SubMenu? _borderRadiusSubMenu;
        private HotkeySubMenu? _hotkeySubMenu;
        private ColorPickerPopup? _colorPickerPopup;
        private System.Windows.Threading.DispatcherTimer? _clickDetectionTimer;
        public DateTime LastHideTime { get; private set; } = DateTime.Now.AddYears(-1);

        public TrayMenu()
        {
            InitializeComponent();
            _clickDetectionTimer = new System.Windows.Threading.DispatcherTimer();
            _clickDetectionTimer.Interval = TimeSpan.FromMilliseconds(50);
            _clickDetectionTimer.Tick += ClickDetectionTimer_Tick;

            InitializeSubMenus();
        }

        private void InitializeSubMenus()
        {
            // Icon Size SubMenu
            _iconSizeSubMenu = new SubMenu();
            _iconSizeSubMenu.AddMenuItem("Small (30px)", "30");
            _iconSizeSubMenu.AddMenuItem("Medium (50px)", "50");
            _iconSizeSubMenu.AddMenuItem("Large (80px)", "80");
            _iconSizeSubMenu.ItemClicked += (s, value) =>
            {
                IconSizeChanged?.Invoke(this, int.Parse(value));
                HideAllSubMenus();
            };

            _iconSizeSubMenu.ItemClicked += (s, value) =>
            {
                IconSizeChanged?.Invoke(this, int.Parse(value));
                HideAllSubMenus();
            };

            // Change Icon SubMenu
            _changeIconSubMenu = new SubMenu();
            _changeIconSubMenu.AddMenuItem("Capybara", "capy.gif");
            _changeIconSubMenu.AddMenuItem("Cat", "cat.gif");
            _changeIconSubMenu.AddMenuItem("Guinea Pig", "guinea.gif");
            _changeIconSubMenu.AddMenuItem("Import Custom...", "import");
            
            _changeIconSubMenu.ItemClicked += (s, value) =>
            {
                if (value == "import")
                {
                    // Trigger import logic
                    using (var openFileDialog = new OpenFileDialog())
                    {
                        openFileDialog.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.gif;*.bmp|All Files|*.*";
                        if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            PetIconChanged?.Invoke(this, openFileDialog.FileName);
                        }
                    }
                }
                else
                {
                    // Construct full path for presets
                    string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", value);
                    PetIconChanged?.Invoke(this, fullPath);
                }
                HideAllSubMenus();
            };

            // Icon Position SubMenu
            _iconPositionSubMenu = new SubMenu();
            _iconPositionSubMenu.AddMenuItem("Left", "Left");
            _iconPositionSubMenu.AddMenuItem("Center", "Center");
            _iconPositionSubMenu.AddMenuItem("Right", "Right");
            _iconPositionSubMenu.ItemClicked += (s, value) =>
            {
                IconPositionChanged?.Invoke(this, value);
                HideAllSubMenus();
            };

            // Border Thickness SubMenu
            _borderThicknessSubMenu = new SubMenu();
            _borderThicknessSubMenu.AddMenuItem("Thin (2px)", "2");
            _borderThicknessSubMenu.AddMenuItem("Normal (4px)", "4");
            _borderThicknessSubMenu.AddMenuItem("Thick (8px)", "8");
            _borderThicknessSubMenu.ItemClicked += (s, value) =>
            {
                BorderThicknessChanged?.Invoke(this, int.Parse(value));
                HideAllSubMenus();
            };

            // Border Radius SubMenu
            _borderRadiusSubMenu = new SubMenu();
            _borderRadiusSubMenu.AddMenuItem("None (0px)", "0");
            _borderRadiusSubMenu.AddMenuItem("Small (4px)", "4");
            _borderRadiusSubMenu.AddMenuItem("Medium (8px)", "8");
            _borderRadiusSubMenu.AddMenuItem("Large (16px)", "16");
            _borderRadiusSubMenu.ItemClicked += (s, value) =>
            {
                BorderRadiusChanged?.Invoke(this, int.Parse(value));
                HideAllSubMenus();
            };

            // Color Picker Popup
            _colorPickerPopup = new ColorPickerPopup();
            _colorPickerPopup.ColorSelected += (s, color) =>
            {
                var brush = new System.Windows.Media.SolidColorBrush(color);
                BorderColorClicked?.Invoke(this, EventArgs.Empty);
                BorderColorChanged?.Invoke(this, brush);
                HideAllSubMenus();
            };

            // Hotkey SubMenu
            _hotkeySubMenu = new HotkeySubMenu();
            _hotkeySubMenu.HotkeyChanged += (s, e) =>
            {
                HotkeyChanged?.Invoke(this, e);
                // Don't close tray menu immediately? Or do? Maybe stay open to show it works
                // But HideAllSubMenus is typical
                HideAllSubMenus();
            };
        }

        public void SetStates(bool showPetIcon, bool showBorder, bool runOnStartup)
        {
            _showPetIcon = showPetIcon;
            _showBorder = showBorder;
            _runOnStartup = runOnStartup;
            UpdateVisuals();
        }

        public void UpdateHotkeyDisplay(uint modifiers, uint key)
        {
            _hotkeySubMenu?.SetCurrentHotkey(modifiers, key);
        }

        private void UpdateVisuals()
        {
            ChkShowPetIcon.IsChecked = _showPetIcon;
            ChkShowBorder.IsChecked = _showBorder;
            ChkRunOnStartup.IsChecked = _runOnStartup;
        }

        private void ClickDetectionTimer_Tick(object? sender, EventArgs e)
        {
            if (!this.IsVisible) return;

            // Check if any mouse button is pressed
            bool mouseButtonDown = (Pinnie.Interop.Win32.GetAsyncKeyState(Pinnie.Interop.Win32.VK_LBUTTON) & 0x8000) != 0 ||
                                 (Pinnie.Interop.Win32.GetAsyncKeyState(Pinnie.Interop.Win32.VK_RBUTTON) & 0x8000) != 0 ||
                                 (Pinnie.Interop.Win32.GetAsyncKeyState(Pinnie.Interop.Win32.VK_MBUTTON) & 0x8000) != 0;

            if (mouseButtonDown && !this.IsMouseOver && 
                !(_changeIconSubMenu?.IsMouseOver ?? false) &&
                !(_iconSizeSubMenu?.IsMouseOver ?? false) && 
                !(_iconPositionSubMenu?.IsMouseOver ?? false) &&
                !(_borderThicknessSubMenu?.IsMouseOver ?? false) &&
                !(_borderRadiusSubMenu?.IsMouseOver ?? false) &&
                !(_colorPickerPopup?.IsMouseOver ?? false) &&
                !(_hotkeySubMenu?.IsMouseOver ?? false))
            {
                this.Hide();
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            IntPtr exStyle = Pinnie.Interop.Win32.GetWindowLongPtr(hwnd, Pinnie.Interop.Win32.GWL_EXSTYLE);
            Pinnie.Interop.Win32.SetWindowLong(hwnd, Pinnie.Interop.Win32.GWL_EXSTYLE, 
                new IntPtr(exStyle.ToInt64() | (long)Pinnie.Interop.Win32.WS_EX_NOACTIVATE | (long)Pinnie.Interop.Win32.WS_EX_TOOLWINDOW));
        }

        public void ShowAtMouse()
        {
            var mousePos = System.Windows.Forms.Cursor.Position;
            
            DpiScale dpi = VisualTreeHelper.GetDpi(this);
            double scaleX = dpi.DpiScaleX;
            double scaleY = dpi.DpiScaleY;

            double wpfX = mousePos.X / scaleX;
            double wpfY = mousePos.Y / scaleY;

            double height = this.ActualHeight > 0 ? this.ActualHeight : 300;

            this.Left = wpfX - this.Width + 20;
            this.Top = wpfY - height - 10;

            if (this.Top < 0) this.Top = 10;
            if (this.Left < 0) this.Left = 10;

            this.Show();
            _clickDetectionTimer?.Start();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            // Don't hide if a submenu or color picker is being shown
            if (_changeIconSubMenu?.IsVisible == true ||
                _iconSizeSubMenu?.IsVisible == true || _iconPositionSubMenu?.IsVisible == true ||
                _borderThicknessSubMenu?.IsVisible == true || _borderRadiusSubMenu?.IsVisible == true || 
                _colorPickerPopup?.IsVisible == true || _hotkeySubMenu?.IsVisible == true)
                return;
            
            this.Hide();
        }

        private void HideAllSubMenus()
        {
            _changeIconSubMenu?.Hide();
            _iconSizeSubMenu?.Hide();
            _iconPositionSubMenu?.Hide();
            _borderThicknessSubMenu?.Hide();
            _borderRadiusSubMenu?.Hide();
            _colorPickerPopup?.Hide();
            _hotkeySubMenu?.Hide();
        }

        public new void Hide()
        {
            if (!this.IsVisible) return;
            LastHideTime = DateTime.Now;
            _clickDetectionTimer?.Stop();
            HideAllSubMenus();
            base.Hide();
        }

        private void BtnShowPetIcon_Click(object sender, RoutedEventArgs e)
        {
            _showPetIcon = !_showPetIcon;
            UpdateVisuals();
            ShowPetIconToggleClicked?.Invoke(this, _showPetIcon);
        }

        private void BtnShowBorder_Click(object sender, RoutedEventArgs e)
        {
            _showBorder = !_showBorder;
            UpdateVisuals();
            ShowBorderToggleClicked?.Invoke(this, _showBorder);
        }

        private void BtnChangeIcon_Click(object sender, RoutedEventArgs e)
        {
            HideAllSubMenus();
            _changeIconSubMenu?.ShowNextTo(BtnChangeIcon, this);
        }

        private void BtnChangeIcon_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HideAllSubMenus();
            _changeIconSubMenu?.ShowNextTo(BtnChangeIcon, this);
        }

        private void BtnIconSize_Click(object sender, RoutedEventArgs e)
        {
            HideAllSubMenus();
            _iconSizeSubMenu?.ShowNextTo(BtnIconSize, this);
        }

        private void BtnIconSize_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HideAllSubMenus();
            _iconSizeSubMenu?.ShowNextTo(BtnIconSize, this);
        }

        private void BtnIconPosition_Click(object sender, RoutedEventArgs e)
        {
            HideAllSubMenus();
            _iconPositionSubMenu?.ShowNextTo(BtnIconPosition, this);
        }

        private void BtnIconPosition_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HideAllSubMenus();
            _iconPositionSubMenu?.ShowNextTo(BtnIconPosition, this);
        }

        private void BtnBorderThickness_Click(object sender, RoutedEventArgs e)
        {
            HideAllSubMenus();
            _borderThicknessSubMenu?.ShowNextTo(BtnBorderThickness, this);
        }

        private void BtnBorderThickness_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HideAllSubMenus();
            _borderThicknessSubMenu?.ShowNextTo(BtnBorderThickness, this);
        }

        private void BtnBorderRadius_Click(object sender, RoutedEventArgs e)
        {
            HideAllSubMenus();
            _borderRadiusSubMenu?.ShowNextTo(BtnBorderRadius, this);
        }

        private void BtnBorderRadius_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HideAllSubMenus();
            _borderRadiusSubMenu?.ShowNextTo(BtnBorderRadius, this);
        }

        private void BtnBorderColor_Click(object sender, RoutedEventArgs e)
        {
            HideAllSubMenus();
            _colorPickerPopup?.ShowNextTo(BtnBorderColor, this);
        }

        private void BtnBorderColor_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HideAllSubMenus();
            _colorPickerPopup?.ShowNextTo(BtnBorderColor, this);
        }

        private void BtnRunOnStartup_Click(object sender, RoutedEventArgs e)
        {
            _runOnStartup = !_runOnStartup;
            UpdateVisuals();
            StartupToggleClicked?.Invoke(this, _runOnStartup);
        }

        private void BtnHotkey_Click(object sender, RoutedEventArgs e)
        {
             HideAllSubMenus();
            _hotkeySubMenu?.ShowNextTo(BtnHotkey, this);
        }

        private void BtnHotkey_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
             HideAllSubMenus();
            _hotkeySubMenu?.ShowNextTo(BtnHotkey, this);
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            ExitClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
