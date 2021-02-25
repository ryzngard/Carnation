using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
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

            var editorFormatMapService = VSServiceHelpers.GetMefExport<IEditorFormatMapService>();
            var editorFormatMap = editorFormatMapService.GetEditorFormatMap("text");
            editorFormatMap.FormatMappingChanged += (object s, FormatItemsEventArgs e) => UpdateClassifications(e.ChangedItems);

            void UpdateClassifications(ReadOnlyCollection<string> definitionNames)
            {
                _viewModel.OnThemeChanged(definitionNames.ToLookup(name => name));
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

        private void DropDownButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var button = (Button)sender;
            var contextMenu = button.ContextMenu;
            contextMenu.PlacementTarget = button;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        private void DropDownButton_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            e.Handled = true;
        }
    }
}
