using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using Carnation.Helpers;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Win32;
using static Carnation.ClassificationProvider;

namespace Carnation
{
    internal class MainWindowControlViewModel : NotifyPropertyBase
    {
        public MainWindowControlViewModel()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var settingsStore = ThreadHelper.JoinableTaskFactory.Run(() => OptionsHelper.GetWritableSettingsStoreAsync());
            if (settingsStore.TryGetBoolean(OptionsHelper.GeneralSettingsCollectionName, nameof(UseExtraContrastSuggestions), out var useExtraContrastSuggestions))
            {
                UseExtraContrastSuggestions = useExtraContrastSuggestions;
            }
            else
            {
                settingsStore.WriteBoolean(OptionsHelper.GeneralSettingsCollectionName, nameof(UseExtraContrastSuggestions), UseExtraContrastSuggestions);
            }

            ClassificationGridView = CollectionViewSource.GetDefaultView(ClassificationGridItems);
            ClassificationGridView.Filter = o => FilterClassification((ClassificationGridItem)o);

            PropertyChanged += OnPropertyChanged;

            EditForegroundCommand = new RelayCommand<ClassificationGridItem>(OnEditForeground);
            EditBackgroundCommand = new RelayCommand<ClassificationGridItem>(OnEditBackground);
            ToggleIsBoldCommand = new RelayCommand<ClassificationGridItem>(OnToggleIsBold);

            ResetToDefaultsCommand = new RelayCommand<ClassificationGridItem>(OnResetToDefaults);
            UseForegroundSuggestionCommand = new RelayCommand<ClassificationGridItem>(OnUseForegroundSuggestion);

            ResetAllToDefaultsCommand = new RelayCommand(OnResetAllToDefaults);
            UseAllForegroundSuggestionsCommand = new RelayCommand(OnUseAllForegroundSuggestions);
            ExportThemeCommand = new RelayCommand(OnExportTheme);
            ImportThemeCommand = new RelayCommand(OnImportTheme);
            LoadThemeCommand = new RelayCommand<string>(OnLoadTheme);
            FindMoreThemesCommand = new RelayCommand(OnFindMoreThemes);

            foreach (var classificationItem in ClassificationProvider.GridItems)
            {
                ClassificationGridItems.Add(classificationItem);
            }
            UpdateContrastWarnings();

            ClassificationGridView.SortDescriptions.Clear();
            ClassificationGridView.SortDescriptions.Add(new SortDescription(nameof(ClassificationGridItem.Classification), ListSortDirection.Ascending));

            (FontFamily, FontSize) = FontsAndColorsHelper.GetEditorFontInfo();
        }

        #region Properties

        public ClassificationProvider ClassificationProvider { get; } = new ClassificationProvider();
        public ObservableCollection<ClassificationGridItem> ClassificationGridItems { get; } = new ObservableCollection<ClassificationGridItem>();

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

        private bool _useExtraContrastSuggestions;
        public bool UseExtraContrastSuggestions
        {
            get => _useExtraContrastSuggestions;
            set => SetProperty(ref _useExtraContrastSuggestions, value);
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
        public ICommand ToggleIsBoldCommand { get; }

        public ICommand ResetToDefaultsCommand { get; }
        public ICommand UseForegroundSuggestionCommand { get; }

        public ICommand ResetAllToDefaultsCommand { get; }
        public ICommand UseAllForegroundSuggestionsCommand { get; }
        public ICommand ExportThemeCommand { get; }
        public ICommand ImportThemeCommand { get; }
        public ICommand LoadThemeCommand { get; }
        public ICommand FindMoreThemesCommand { get; }

        #endregion

        #region Public Methods
        public void OnThemeChanged(ILookup<string, string> definitionNames)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (definitionNames.Contains("Plain Text"))
            {
                (FontFamily, FontSize) = FontsAndColorsHelper.GetEditorFontInfo();
            }
            
            ClassificationProvider.Refresh(definitionNames);
            UpdateContrastWarnings(definitionNames);
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
            SearchText = string.Join("; ", classifications);
        }
        #endregion

        #region Private Methods

        private void UpdateContrastWarnings(ILookup<string, string> definitionNames = null)
        {
            var minimumContrastRatio = UseExtraContrastSuggestions
                ? ContrastHelpers.AAA_Contrast
                : ContrastHelpers.AA_Contrast;

            foreach (var item in ClassificationGridItems)
            {
                if (definitionNames?.Contains(item.DefinitionName) == false)
                {
                    continue;
                }

                item.HasContrastWarning = item.IsForegroundEditable
                    && item.IsBackgroundEditable
                    && item.ContrastRatio < minimumContrastRatio;
            }
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
                var classifications = SearchText.Split(';')
                    .Select(name => name.Trim())
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToLookup(name => name);

                // If follow cursor is selected, we only want exact matches
                return classifications.Contains(item.Classification);
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

                case nameof(UseExtraContrastSuggestions):
                    ThreadHelper.JoinableTaskFactory.Run(UpdateUseExtraContrastOptionAsync);
                    break;
            }
        }

        private async System.Threading.Tasks.Task UpdateUseExtraContrastOptionAsync()
        {
            var settingsStore = await OptionsHelper.GetWritableSettingsStoreAsync();
            settingsStore.WriteBoolean(OptionsHelper.GeneralSettingsCollectionName, nameof(UseExtraContrastSuggestions), UseExtraContrastSuggestions);
            UpdateContrastWarnings();
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

        private void OnResetToDefaults(ClassificationGridItem item)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            FontsAndColorsHelper.ResetClassificationItem(item);
        }

        private void OnUseForegroundSuggestion(ClassificationGridItem item)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var suggestions = UseExtraContrastSuggestions
                ? ContrastHelpers.FindSimilarAAAColor(item.Foreground, item.Background)
                : ContrastHelpers.FindSimilarAAColor(item.Foreground, item.Background);
            if (suggestions.Length == 0)
            {
                item.HasContrastWarning = false;
                return;
            }

            var topSuggestion = suggestions.OrderBy(suggestion => suggestion.Distance).First();
            item.Foreground = topSuggestion.Color;
        }

        private void OnToggleIsBold(ClassificationGridItem item)
        {
            item.IsBold = !item.IsBold;
        }

        private void OnEditForeground(ClassificationGridItem item)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ShowColorPicker(item);
        }

        private void OnEditBackground(ClassificationGridItem item)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ShowColorPicker(item, true);
        }

        private void ShowColorPicker(ClassificationGridItem item, bool editBackground = false)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var window = new ColorPickerWindow(
                item.Foreground,
                item.Background,
                UseExtraContrastSuggestions,
                FontFamily,
                FontSize,
                editBackground: editBackground);
            if (window.ShowDialog() == true)
            {
                item.Foreground = window.ForegroundColor;
                item.Background = window.BackgroundColor;
            }
        }

        private void OnResetAllToDefaults()
        {
            FontsAndColorsHelper.ResetAllClassificationItems();
            UpdateContrastWarnings();
        }

        private void OnExportTheme()
        {
            var dialog = new SaveFileDialog
            {
                DefaultExt = "vssettings",
                Title = "Export Theme",
                Filter = "Settings Files (*.vssettings)|*.vssettings|All Files (*.*)|*.*",
                AddExtension = true,
                OverwritePrompt = true,
                CheckPathExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                ThemeExporter.Export(dialog.FileName, ClassificationGridItems);
            }
        }

        private void OnImportTheme()
        {
            var dialog = new OpenFileDialog
            {
                DefaultExt = "vssettings",
                Title = "Import Theme",
                Filter = "Settings Files (*.vssettings)|*.vssettings|All Files (*.*)|*.*",
                AddExtension = true,
                CheckPathExists = true,
                CheckFileExists = true,
                Multiselect = false, 
            };

            if (dialog.ShowDialog() == true)
            {
                var operationExecutor = VSServiceHelpers.GetMefExport<IUIThreadOperationExecutor>();
                operationExecutor.Execute(
                    "Carnation",
                    "Loading theme colors...",
                    allowCancellation: false,
                    showProgress: true,
                    (context) =>
                    {
                        ThemeImporter.Import(dialog.FileName, ClassificationGridItems);
                    });
            }
        }

        private void OnLoadTheme(string themeName)
        {
            var operationExecutor = VSServiceHelpers.GetMefExport<IUIThreadOperationExecutor>();
            operationExecutor.Execute(
                "Carnation",
                "Loading theme colors...",
                allowCancellation: false,
                showProgress: true,
                (context) =>
                {
                    using var themeStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Carnation.Resources.Themes.{themeName}.vssettings");
                    var themeSettings = XDocument.Load(themeStream);
                    ThemeImporter.Import(themeSettings, ClassificationGridItems);
                });
        }

        private void OnFindMoreThemes()
        {
            Process.Start("https://studiostyl.es");
        }

        private void OnUseAllForegroundSuggestions()
        {
            var operationExecutor = VSServiceHelpers.GetMefExport<IUIThreadOperationExecutor>();
            operationExecutor.Execute(
                "Carnation",
                "Applying all foreground color suggestions...",
                allowCancellation: false,
                showProgress: true,
                (context) =>
                {
                    foreach (var item in ClassificationGridItems)
                    {
                        if (item.HasContrastWarning)
                        {
                            var suggestions = UseExtraContrastSuggestions
                                ? ContrastHelpers.FindSimilarAAAColor(item.Foreground, item.Background)
                                : ContrastHelpers.FindSimilarAAColor(item.Foreground, item.Background);
                            if (suggestions.Length == 0)
                            {
                                item.HasContrastWarning = false;
                                continue;
                            }

                            var topSuggestion = suggestions.OrderBy(suggestion => suggestion.Distance).First();
                            item.Foreground = topSuggestion.Color;
                        }
                    }
                });
        }

        #endregion
    }
}
