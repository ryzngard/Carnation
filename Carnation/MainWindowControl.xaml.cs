using System;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;

namespace Carnation
{
    /// <summary>
    /// Interaction logic for MainWindowControl.
    /// </summary>
    public partial class MainWindowControl : UserControl, IDisposable
    {
        private ActiveWindowTracker _activeWindowTracker;
        private readonly MainWindowControlViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowControl"/> class.
        /// </summary>
        public MainWindowControl()
        {
            DataContext = _viewModel = new MainWindowControlViewModel();
            InitializeComponent();

            _activeWindowTracker = new ActiveWindowTracker();
            _activeWindowTracker.PropertyChanged += ActiveWindowPropertyChanged;

            var classificationFormatMapService = VSServiceHelpers.GetMefExport<IClassificationFormatMapService>();
            var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap("Text Editor MEF Items");
            classificationFormatMap.ClassificationFormatMappingChanged += (object s, EventArgs e) => ReloadClassifications();

            VSColorTheme.ThemeChanged += (ThemeChangedEventArgs _) => ReloadClassifications();

            ReloadClassifications();

            void ReloadClassifications()
            {
                _viewModel.PlainTextForeground = ((SolidColorBrush)classificationFormatMap.DefaultTextProperties.ForegroundBrush).Color;
                _viewModel.PlainTextBackground = ((SolidColorBrush)classificationFormatMap.DefaultTextProperties.BackgroundBrush).Color;
                _viewModel.OnThemeChanged();
            }
        }

        private void ActiveWindowPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            switch (e.PropertyName)
            {
                case nameof(ActiveWindowTracker.SelectedSpan):
                    _viewModel.OnSelectedSpanChanged(_activeWindowTracker.ActiveWpfTextView, _activeWindowTracker.SelectedSpan);
                    break;

                case nameof(ActiveWindowTracker.ActiveWpfTextView):
                    break;
            }
        }

        public void Dispose()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _activeWindowTracker?.Dispose();
            _activeWindowTracker = null;
        }
    }
}
