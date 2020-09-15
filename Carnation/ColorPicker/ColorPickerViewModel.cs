using System.Windows.Media;
using Carnation.Models;

namespace Carnation
{
    internal class ColorPickerViewModel : NotifyPropertyBase
    {
        public ColorPickerViewModel()
        {
            ForegroundColor = new ObservableColor(Colors.Transparent);
            BackgroundColor = new ObservableColor(Colors.Transparent);
            CurrentEditorColor = ForegroundColor;
        }

        private ObservableColor _currentEditorColor;
        public ObservableColor CurrentEditorColor
        {
            get => _currentEditorColor;
            set => SetProperty(ref _currentEditorColor, value);
        }

        private ObservableColor _backgroundColor;
        public ObservableColor BackgroundColor
        {
            get => _backgroundColor;
            set => SetProperty(ref _backgroundColor, value);
        }

        private ObservableColor _foregroundColor;
        public ObservableColor ForegroundColor
        {
            get => _foregroundColor;
            set => SetProperty(ref _foregroundColor, value);
        }

        public void SetForegroundColor(Color color)
        {
            ForegroundColor.Color = color;
        }

        public void SetBackgroundColor(Color color)
        {                
            BackgroundColor.Color = color;
        }
    }
}
