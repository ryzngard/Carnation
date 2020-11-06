using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using Carnation.Helpers;
using Carnation.Models;

namespace Carnation
{
    internal class ColorPickerViewModel : NotifyPropertyBase
    {
        private readonly TimeLimitedAction _calculateSuggestedColors;
        public ColorPickerViewModel()
        {
            ForegroundColor = new ObservableColor(Colors.Transparent);
            BackgroundColor = new ObservableColor(Colors.Transparent);
            CurrentEditorColor = ForegroundColor;

            ForegroundColor.PropertyChanged += ForegroundColor_PropertyChanged;
            BackgroundColor.PropertyChanged += BackgroundColor_PropertyChanged;

            _calculateSuggestedColors = new TimeLimitedAction(() =>
            {
                var aaSuggestion = ContrastHelpers.FindSimilarAAColor(ForegroundColor.Color, BackgroundColor.Color);
                var aaaSuggestion = ContrastHelpers.FindSimilarAAAColor(ForegroundColor.Color, BackgroundColor.Color);

                var suggestedColors = aaaSuggestion.Concat(aaSuggestion);

                SuggestedColors.Clear();
                foreach (var suggestion in suggestedColors)
                {
                    SuggestedColors.Add(suggestion.Color);
                }

                ShowSuggestedColors = SuggestedColors.Any();
            }, TimeSpan.FromSeconds(1));

            CalculateContrast();
        }

        private void CalculateContrast()
        {
            ContrastRatio = ColorHelpers.GetContrast(ForegroundColor.Color, BackgroundColor.Color);
            _calculateSuggestedColors.TryToExecute();
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
            set
            { 
                if (SetProperty(ref _currentEditorColor, value))
                {
                    NotifyPropertyChanged(nameof(IsForegroundBeingEdited));
                }
            }
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

        public ObservableCollection<Color> SuggestedColors { get; } = new ObservableCollection<Color>();
        private bool _showSuggestedColors;
        public bool ShowSuggestedColors
        {
            get => _showSuggestedColors;
            set => SetProperty(ref _showSuggestedColors, value);
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

        public bool IsForegroundBeingEdited => CurrentEditorColor == ForegroundColor;
    }
}
