using System.Windows.Media;

namespace Carnation
{
    public class ClassificationGridItem : ColorItemBase
    {
        private string _classification;
        public string Classification
        {
            get => _classification;
            set => SetProperty(ref _classification, value);
        }

        private string _definitionName;
        public string DefinitionName
        {
            get => _definitionName;
            set => SetProperty(ref _definitionName, value);
        }

        public string Sample => "Sample Text";

        public ClassificationGridItem(
            string classification,
            string definitionName,
            Color foregroundColor,
            Color backgroundColor,
            bool isBold)
            : base(foregroundColor, backgroundColor, isBold)
        {
            _classification = classification;
            _definitionName = definitionName;
        }

        public override string ToString()
            => Classification;
    }
}
