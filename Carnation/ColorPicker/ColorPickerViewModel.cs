using System;
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

            ForegroundColor.PropertyChanged += ForegroundColor_PropertyChanged;
            BackgroundColor.PropertyChanged += BackgroundColor_PropertyChanged;

            CalculateContrast();
        }

        private void CalculateContrast()
        {
            ContrastRatio = ColorHelpers.GetContrast(ForegroundColor.Color, BackgroundColor.Color);
        }

        private void BackgroundColor_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            CalculateContrast();
        }

        private void ForegroundColor_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            CalculateContrast();
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

        private double _contrastRatio;
        public double ContrastRatio
        {
            get => _contrastRatio;
            set => SetProperty(ref _contrastRatio, value);
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
