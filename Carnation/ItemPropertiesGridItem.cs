using System.Windows.Media;

namespace Carnation
{
    class ItemPropertiesGridItem : NotifyPropertyBase
    {
        public string Text => "Sample Text";

        private Color _foreground;
        public Color Foreground
        {
            get => _foreground;
            set => SetProperty(ref _foreground, value);
        }

        private Color _background;
        public Color Background
        {
            get => _background;
            set => SetProperty(ref _background, value);
        }

        public ItemPropertiesGridItem(
            Color foreground,
            Color background)
        {
            _foreground = foreground;
            _background = background;
        }
    }
}
