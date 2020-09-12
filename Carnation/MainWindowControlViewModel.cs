using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Media;

namespace Carnation
{
    class MainWindowControlViewModel : NotifyPropertyBase
    {
        public MainWindowControlViewModel()
        {
            ClassificationGridView = CollectionViewSource.GetDefaultView(ClassificationGridItems);
            ClassificationGridView.Filter = o => FilterClassification((ClassificationGridItem)o);

            PropertyChanged += OnPropertyChanged;
        }

        #region Properties
        private static readonly ItemPropertiesGridItem[] s_defaultPropertiesGridItems = new[]
        {
            new ItemPropertiesGridItem(Colors.White, Colors.Black),
            new ItemPropertiesGridItem(Colors.DarkRed, Colors.White)
        };

        private static readonly ClassificationGridItem[] s_defaultClassificationGridItems = new[]
        {
            new ClassificationGridItem("Test Classification 1", Colors.White, Colors.Black, "Content Type Test 1"),
            new ClassificationGridItem("Test Classification 2", Colors.White, Colors.Black, "Content Type Test 2"),
            new ClassificationGridItem("Test Classification 3", Colors.White, Colors.Black, "Content Type Test 3"),
        };

        public ObservableCollection<ClassificationGridItem> ClassificationGridItems { get; } = new ObservableCollection<ClassificationGridItem>(s_defaultClassificationGridItems);
        public ObservableCollection<ItemPropertiesGridItem> ItemPropertiesGridItems { get; } = new ObservableCollection<ItemPropertiesGridItem>(s_defaultPropertiesGridItems);

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

        public ICollectionView ClassificationGridView { get; }
        #endregion

        #region Private Methods
        private bool FilterClassification(ClassificationGridItem item)
        {
            if (FollowCursorSelected || item is null)
            {
                return true;
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
            }
        }

        private void OnSearchTextChanged()
        {
            ClassificationGridView.Refresh();
        }

        private void OnFollowCursorChanged()
        {
            SearchTextEnabled = !FollowCursorSelected;
        }
        #endregion
    }
}
