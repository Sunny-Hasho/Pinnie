using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Pinnie
{
    public partial class ColorPickerWindow : Window
    {
        public System.Windows.Media.Color? SelectedColor { get; private set; }
        private System.Windows.Media.Color[] _customColors = new System.Windows.Media.Color[16];

        private static readonly System.Windows.Media.Color[] BasicColors = new System.Windows.Media.Color[]
        {
            // Row 1
            System.Windows.Media.Color.FromRgb(255, 128, 128), System.Windows.Media.Color.FromRgb(255, 255, 128), System.Windows.Media.Color.FromRgb(128, 255, 128), System.Windows.Media.Color.FromRgb(0, 255, 128),
            System.Windows.Media.Color.FromRgb(128, 255, 255), System.Windows.Media.Color.FromRgb(0, 128, 255), System.Windows.Media.Color.FromRgb(255, 128, 192), System.Windows.Media.Color.FromRgb(255, 128, 255),
            
            // Row 2
            System.Windows.Media.Color.FromRgb(255, 0, 0), System.Windows.Media.Color.FromRgb(255, 255, 0), System.Windows.Media.Color.FromRgb(128, 255, 0), System.Windows.Media.Color.FromRgb(0, 255, 64),
            System.Windows.Media.Color.FromRgb(0, 255, 255), System.Windows.Media.Color.FromRgb(0, 128, 192), System.Windows.Media.Color.FromRgb(128, 128, 192), System.Windows.Media.Color.FromRgb(255, 0, 255),
            
            // Row 3
            System.Windows.Media.Color.FromRgb(128, 64, 64), System.Windows.Media.Color.FromRgb(255, 128, 64), System.Windows.Media.Color.FromRgb(0, 255, 0), System.Windows.Media.Color.FromRgb(0, 128, 128),
            System.Windows.Media.Color.FromRgb(0, 64, 128), System.Windows.Media.Color.FromRgb(128, 128, 255), System.Windows.Media.Color.FromRgb(128, 0, 64), System.Windows.Media.Color.FromRgb(255, 0, 128),
            
            // Row 4
            System.Windows.Media.Color.FromRgb(128, 0, 0), System.Windows.Media.Color.FromRgb(255, 128, 0), System.Windows.Media.Color.FromRgb(0, 128, 0), System.Windows.Media.Color.FromRgb(0, 128, 64),
            System.Windows.Media.Color.FromRgb(0, 0, 255), System.Windows.Media.Color.FromRgb(0, 0, 160), System.Windows.Media.Color.FromRgb(128, 0, 128), System.Windows.Media.Color.FromRgb(128, 0, 255),
            
            // Row 5
            System.Windows.Media.Color.FromRgb(64, 0, 0), System.Windows.Media.Color.FromRgb(128, 64, 0), System.Windows.Media.Color.FromRgb(0, 64, 0), System.Windows.Media.Color.FromRgb(0, 64, 64),
            System.Windows.Media.Color.FromRgb(0, 0, 128), System.Windows.Media.Color.FromRgb(0, 0, 64), System.Windows.Media.Color.FromRgb(64, 0, 64), System.Windows.Media.Color.FromRgb(64, 0, 128),
            
            // Row 6
            System.Windows.Media.Color.FromRgb(0, 0, 0), System.Windows.Media.Color.FromRgb(128, 128, 0), System.Windows.Media.Color.FromRgb(128, 128, 64), System.Windows.Media.Color.FromRgb(128, 128, 128),
            System.Windows.Media.Color.FromRgb(64, 128, 128), System.Windows.Media.Color.FromRgb(192, 192, 192), System.Windows.Media.Color.FromRgb(64, 0, 64), System.Windows.Media.Color.FromRgb(255, 255, 255)
        };

        public ColorPickerWindow()
        {
            InitializeComponent();
            InitializeColors();
        }

        private void InitializeColors()
        {
            // Add basic colors
            foreach (var color in BasicColors)
            {
                var button = new System.Windows.Controls.Button
                {
                    Style = (Style)this.Resources["ColorButtonStyle"],
                    Background = new SolidColorBrush(color),
                    Tag = color
                };
                button.Click += ColorButton_Click;
                BasicColorsGrid.Children.Add(button);
            }

            // Add custom color slots (initially white)
            for (int i = 0; i < 16; i++)
            {
                _customColors[i] = System.Windows.Media.Colors.White;
                var button = new System.Windows.Controls.Button
                {
                    Style = (Style)this.Resources["CustomColorButtonStyle"],
                    Background = new SolidColorBrush(System.Windows.Media.Colors.White),
                    Tag = i
                };
                button.Click += CustomColorButton_Click;
                CustomColorsGrid.Children.Add(button);
            }
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is System.Windows.Media.Color color)
            {
                SelectedColor = color;
                DialogResult = true;
                Close();
            }
        }

        private void CustomColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is int index)
            {
                SelectedColor = _customColors[index];
                DialogResult = true;
                Close();
            }
        }

        private void BtnDefineCustomColors_Click(object sender, RoutedEventArgs e)
        {
            // Use the old ColorDialog for advanced color picking
            using (var colorDialog = new System.Windows.Forms.ColorDialog())
            {
                colorDialog.AllowFullOpen = true;
                colorDialog.FullOpen = true;
                
                if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var color = System.Windows.Media.Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B);
                    
                    // Find first empty custom color slot or use the last one
                    int slotIndex = 0;
                    for (int i = 0; i < _customColors.Length; i++)
                    {
                        if (_customColors[i] == System.Windows.Media.Colors.White)
                        {
                            slotIndex = i;
                            break;
                        }
                        slotIndex = i;
                    }
                    
                    // Update custom color
                    _customColors[slotIndex] = color;
                    if (CustomColorsGrid.Children[slotIndex] is System.Windows.Controls.Button button)
                    {
                        button.Background = new SolidColorBrush(color);
                    }
                    
                    SelectedColor = color;
                    DialogResult = true;
                    Close();
                }
            }
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            // If no color selected, default to black
            if (SelectedColor == null)
            {
                SelectedColor = System.Windows.Media.Colors.Black;
            }
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
