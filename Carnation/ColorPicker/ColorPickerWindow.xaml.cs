using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Carnation
{
    /// <summary>
    /// Interaction logic for ColorPickerWindow.xaml
    /// </summary>
    public partial class ColorPickerWindow : Window
    {
        public Color ForegroundColor => ColorPicker.ForegroundColor;
        public Color BackgroundColor => ColorPicker.BackgroundColor;

        public ColorPickerWindow(Color foregroundColor, Color backgroundColor, bool editBackground = false)
        {
            InitializeComponent();

            ColorPicker.ForegroundColor = foregroundColor;
            ColorPicker.BackgroundColor = backgroundColor;
            ColorPicker.EditBackgroundColor = editBackground;

            // Focus the first tab item when the window loads
            Loaded += (s, e) => MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
