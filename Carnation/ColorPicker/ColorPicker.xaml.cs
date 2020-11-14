using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Carnation.Models;

namespace Carnation
{
    /// <summary>
    /// Interaction logic for ColorPicker.xaml
    /// </summary>
    public partial class ColorPicker : UserControl
    {
        public static readonly DependencyProperty ForegroundColorProperty = DependencyProperty.Register(
            nameof(ForegroundColor),
            typeof(Color),
            typeof(ColorPicker),
            new PropertyMetadata(Colors.Red, OnForegroundColorChanged));

        public static readonly DependencyProperty BackgroundColorProperty = DependencyProperty.Register(
            nameof(BackgroundColor),
            typeof(Color),
            typeof(ColorPicker),
            new PropertyMetadata(Colors.Red, OnBackgroundColorChanged));

        public static readonly DependencyProperty EditBackgroundColorProperty = DependencyProperty.Register(
            nameof(EditBackgroundColor),
            typeof(bool),
            typeof(ColorPicker),
            new PropertyMetadata(false, OnEditBackgroundChanged));

        public static readonly DependencyProperty UseExtraContrastSuggestionsProperty = DependencyProperty.Register(
            nameof(UseExtraContrastSuggestions),
            typeof(bool),
            typeof(ColorPicker),
            new PropertyMetadata(false, OnUseExtraContrastSuggestionsChanged));

        public static readonly DependencyProperty SampleTextFontFamilyProperty = DependencyProperty.Register(
            nameof(SampleTextFontFamily),
            typeof(FontFamily),
            typeof(ColorPicker),
            new PropertyMetadata(FontsAndColorsHelper.DefaultFontInfo.FontFamily, OnSampleTextFontFamilyChanged));

        public static readonly DependencyProperty SampleTextFontSizeProperty = DependencyProperty.Register(
            nameof(SampleTextFontSize),
            typeof(double),
            typeof(ColorPicker),
            new PropertyMetadata(FontsAndColorsHelper.DefaultFontInfo.FontSize, OnSampleTextFontSizeChanged));

        private readonly ColorPickerViewModel _viewModel;

        public ColorPicker()
        {
            DataContext = _viewModel = new ColorPickerViewModel();
            InitializeComponent();

            _viewModel.PropertyChanged += ViewModelPropertyChanged;
            _viewModel.ForegroundColor.PropertyChanged += OnViewModelForegroundColorChanged;
            _viewModel.BackgroundColor.PropertyChanged += OnViewModelBackgroundColorChanged;
        }

        public void SetColor(Color color)
        {
            _viewModel.CurrentEditorColor.Color = color;
        }

        public Color GetColor() => _viewModel.CurrentEditorColor.Color;

        private void ViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ColorPickerViewModel.ForegroundColor):
                    if (_viewModel.ForegroundColor != null)
                    {
                        _viewModel.ForegroundColor.PropertyChanged += OnViewModelForegroundColorChanged;
                    }
                    break;

                case nameof(ColorPickerViewModel.BackgroundColor):
                    if (_viewModel.BackgroundColor != null)
                    {
                        _viewModel.BackgroundColor.PropertyChanged += OnViewModelBackgroundColorChanged;
                    }
                    break;

                case nameof(ColorPickerViewModel.CurrentEditorColor):
                    EditBackgroundColor = _viewModel.CurrentEditorColor == _viewModel.BackgroundColor;
                    break;
            }
        }

        private void OnViewModelForegroundColorChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ObservableColor.Color))
            {
                ForegroundColor = ((ObservableColor)sender).Color;
            }
        }

        private void OnViewModelBackgroundColorChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ObservableColor.Color))
            {
                BackgroundColor = ((ObservableColor)sender).Color;
            }
        }

        public Color BackgroundColor
        {
            get => (Color)GetValue(BackgroundColorProperty);
            set => SetValue(BackgroundColorProperty, value);
        }

        public Color ForegroundColor
        {
            get => (Color)GetValue(ForegroundColorProperty);
            set => SetValue(ForegroundColorProperty, value);
        }

        public bool EditBackgroundColor
        {
            get => (bool)GetValue(EditBackgroundColorProperty);
            set => SetValue(EditBackgroundColorProperty, value);
        }

        public bool UseExtraContrastSuggestions
        {
            get => (bool)GetValue(UseExtraContrastSuggestionsProperty);
            set => SetValue(UseExtraContrastSuggestionsProperty, value);
        }

        public FontFamily SampleTextFontFamily
        {
            get => (FontFamily)GetValue(SampleTextFontFamilyProperty);
            set => SetValue(SampleTextFontFamilyProperty, value);
        }

        public double SampleTextFontSize
        {
            get => (double)GetValue(SampleTextFontSizeProperty);
            set => SetValue(SampleTextFontSizeProperty, value);
        }

        private void SelectAllText(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        private static void OnForegroundColorChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var colorPicker = (ColorPicker)o;
            var vm = (ColorPickerViewModel)colorPicker.DataContext;
            vm.SetForegroundColor((Color)e.NewValue);
        }

        private static void OnBackgroundColorChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var colorPicker = (ColorPicker)o;
            var vm = (ColorPickerViewModel)colorPicker.DataContext;
            vm.SetBackgroundColor((Color)e.NewValue);
        }

        private static void OnEditBackgroundChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var colorPicker = (ColorPicker)o;
            var vm = (ColorPickerViewModel)colorPicker.DataContext;

            if ((bool)e.NewValue)
            {
                vm.CurrentEditorColor = vm.BackgroundColor;
            }
            else
            {
                vm.CurrentEditorColor = vm.ForegroundColor;
            }
        }

        private static void OnUseExtraContrastSuggestionsChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var colorPicker = (ColorPicker)o;
            var vm = (ColorPickerViewModel)colorPicker.DataContext;
            vm.UseExtraContrastSuggestions = (bool)e.NewValue;
        }

        private static void OnSampleTextFontFamilyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var colorPicker = (ColorPicker)o;
            var vm = (ColorPickerViewModel)colorPicker.DataContext;
            vm.SampleTextFontFamily = (FontFamily)e.NewValue;
        }

        private static void OnSampleTextFontSizeChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var colorPicker = (ColorPicker)o;
            var vm = (ColorPickerViewModel)colorPicker.DataContext;
            vm.SampleTextFontSize = (double)e.NewValue;
        }

        private void SuggestedColor_Selected(object sender, RoutedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            if (comboBox.SelectedItem is Color selectedColor)
            {
                _viewModel.ForegroundColor.Color = selectedColor;
            }
        }
    }
}
