using System.Windows.Media;

namespace Carnation
{
    internal class ClassificationGridItem : ColorItemBase
    {
        private string _classification;
        public string Classification
        {
            get => _classification;
            set => SetProperty(ref _classification, value);
        }

        public string Sample => "Sample Text";

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
            : base(foregroundColor, backgroundColor)
        {
            _classification = classification;
            _contentType = contentType;
        }
    }
}
