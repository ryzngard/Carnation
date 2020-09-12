using System.Windows.Media;

namespace Carnation
{
    internal class ClassificationGridItem : NotifyPropertyBase
    {
        private string _classification;
        public string Classification
        {
            get => _classification;
            set => SetProperty(ref _classification, value);
        }

        public string Sample => "Sample Text";

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

        private string _contentType;
        public string ContentType
        {
            get => _contentType;
            set => SetProperty(ref _contentType, value);
        }

        public ClassificationGridItem(
            string classification,
            Color foregroundColor,
            Color backgroundColor,
            string contentType)
        {
            _classification = classification;
            _foreground = foregroundColor;
            _background = backgroundColor;
            _contentType = contentType;
        }
    }
}
