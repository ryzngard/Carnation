using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Carnation
{
    /// <summary>
    /// Interaction logic for ColorPickerWindow.xaml
    /// </summary>
    public partial class ColorPickerWindow : DialogWindow
    {
        public Color ForegroundColor => ColorPicker.ForegroundColor;
        public Color BackgroundColor => ColorPicker.BackgroundColor;

        private int _saveColorIndex = 0;

        public ColorPickerWindow(Color foregroundColor, Color backgroundColor, bool editBackground = false)
        {
            InitializeComponent();

            ColorPicker.ForegroundColor = foregroundColor;
            ColorPicker.BackgroundColor = backgroundColor;
            ColorPicker.EditBackgroundColor = editBackground;

            SavedColors.Colors = ThreadHelper.JoinableTaskFactory.Run(() => SavedColorsManager.GetColorsAsync());

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

        private void ColorPaletteSelected(object sender, ColorPalleteSelectedArgs args)
        {
            ColorPicker.SetColor(args.Color);

            if (sender == SavedColors)
            {
                _saveColorIndex = args.Index;
            }
        }

        private void SaveColor_Click(object _, RoutedEventArgs _1)
        {
            var color = ColorPicker.GetColor();

            var currentColors = SavedColors.Colors.ToBuilder();
            currentColors[_saveColorIndex] = color;

            SavedColors.Colors = currentColors.ToImmutable();

            ThreadHelper.JoinableTaskFactory.Run(() => SavedColorsManager.SaveColorAsync(color, _saveColorIndex));

            if (++_saveColorIndex >= SavedColors.Colors.Length)
            {
                _saveColorIndex = 0;
            }
        }
    }
}
