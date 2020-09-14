using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Carnation
{
    internal class MainWindowControlViewModel : NotifyPropertyBase
    {
        public MainWindowControlViewModel()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ClassificationGridView = CollectionViewSource.GetDefaultView(ClassificationGridItems);
            ClassificationGridView.Filter = o => FilterClassification((ClassificationGridItem)o);

            PropertyChanged += OnPropertyChanged;

            EditForegroundCommand = new RelayCommand<ClassificationGridItem>(OnEditForeground);
            EditBackgroundCommand = new RelayCommand<ClassificationGridItem>(OnEditBackground);
        }

        #region Properties
        private static readonly ItemPropertiesGridItem[] s_defaultPropertiesGridItems = new[]
        {
            new ItemPropertiesGridItem(Colors.White, Colors.Black, false),
            new ItemPropertiesGridItem(Colors.DarkRed, Colors.White, true)
        };

        private static readonly Color[] s_availableColors = new[]
        {
            Colors.White,
            Colors.Red,
            Colors.Green,
            Colors.Blue,
            Colors.Orange,
            Colors.Pink,
            Colors.Black
        };

        public ObservableCollection<ClassificationGridItem> ClassificationGridItems { get; } = new ObservableCollection<ClassificationGridItem>();
        public ObservableCollection<ItemPropertiesGridItem> ItemPropertiesGridItems { get; } = new ObservableCollection<ItemPropertiesGridItem>(s_defaultPropertiesGridItems);
        public ObservableCollection<Color> AvailableColors { get; } = new ObservableCollection<Color>(s_availableColors);

        private ClassificationGridItem _selectedClassification;
        public ClassificationGridItem SelectedClassification
        {
            get => _selectedClassification;
            set => SetProperty(ref _selectedClassification, value);
        }

        private bool _followCursorSelected;
        public bool FollowCursorSelected
        {
            get => _followCursorSelected;
            set => SetProperty(ref _followCursorSelected, value);
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        private bool _searchTextEnabled;
        public bool SearchTextEnabled
        {
            get => _searchTextEnabled;
            set => SetProperty(ref _searchTextEnabled, value);
        }

        private Color _plainTextForeground;
        public Color PlainTextForeground
        {
            get => _plainTextForeground;
            set => SetProperty(ref _plainTextForeground, value);
        }

        private Color _plainTextBackground;
        public Color PlainTextBackground
        {
            get => _plainTextBackground;
            set => SetProperty(ref _plainTextBackground, value);
        }

        private FontFamily _fontFamily;
        public FontFamily FontFamily
        {
            get => _fontFamily;
            set => SetProperty(ref _fontFamily, value);
        }

        private double _fontSize;
        public double FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }

        public ICollectionView ClassificationGridView { get; }

        public Color SelectedItemForeground
        {
            get => SelectedClassification?.Foreground ?? Colors.Transparent;
            set
            {
                if (SelectedClassification.Foreground != value)
                {
                    SelectedClassification.Foreground = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public Color SelectedItemBackground
        {
            get => SelectedClassification?.Background ?? Colors.Transparent;
            set
            {
                if (SelectedClassification.Background != value)
                {
                    SelectedClassification.Background = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public ICommand EditForegroundCommand { get; }
        public ICommand EditBackgroundCommand { get; }
        #endregion

        #region Public Methods
        public void OnThemeChanged()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ReloadClassifications();
        }

        public void OnSelectedSpanChanged(IWpfTextView view, Span? span)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (span is null || view is null)
            {
                return;
            }

            if (!FollowCursorSelected)
            {
                return;
            }

            var classifications = ClassificationHelpers.GetClassificationsForSpan(view, span.Value);
            SearchText = classifications.FirstOrDefault();
        }
        #endregion

        #region Private Methods
        private void ReloadClassifications()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            (FontFamily, FontSize) = FontsAndColorsHelper.GetEditorFontInfo();

            var classificationItems = ClassificationHelpers.GetClassificationNames()
                .Select(names => FontsAndColorsHelper.TryGetItemForClassification(names, PlainTextForeground, PlainTextBackground))
                .OfType<ClassificationGridItem>()
                .ToImmutableArray();

            ClassificationGridItems.Clear();

            foreach (var classificationItem in classificationItems)
            {
                ClassificationGridItems.Add(classificationItem);
            }

            ClassificationGridView.SortDescriptions.Clear();
            ClassificationGridView.SortDescriptions.Add(new SortDescription("Classification", ListSortDirection.Ascending));
        }

        private bool FilterClassification(ClassificationGridItem item)
        {
            if (string.IsNullOrEmpty(SearchText))
            {
                return true;
            }

            if (item is null)
            {
                return false;
            }

            if (FollowCursorSelected)
            {
                // If follow cursor is selected, we only want exact matches
                return item.Classification == SearchText;
            }

            return item.Classification.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) != -1;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(FollowCursorSelected):
                    OnFollowCursorChanged();
                    break;

                case nameof(SearchText):
                    OnSearchTextChanged();
                    break;

                case nameof(SelectedClassification):
                    NotifyPropertyChanged(nameof(SelectedItemBackground));
                    NotifyPropertyChanged(nameof(SelectedItemForeground));
                    break;
            }
        }

        private void OnSearchTextChanged()
        {
            ClassificationGridView.Refresh();
        }

        private void OnFollowCursorChanged()
        {
            SearchTextEnabled = !FollowCursorSelected;
            if (FollowCursorSelected)
            {
                SearchText = string.Empty;
            }
        }

        private void OnEditForeground(ClassificationGridItem item)
        {
            item.Foreground = ShowColorPicker(item.Foreground);
        }

        private void OnEditBackground(ClassificationGridItem item)
        {
            item.Background = ShowColorPicker(item.Background);
        }

        private Color ShowColorPicker(Color color)
        {
            var window = new ColorPickerWindow(color);
            if (window.ShowDialog() == true)
            {
                return window.Color;
            }

            return color;
        }
        #endregion
    }
}
