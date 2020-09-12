using System.Windows.Media;

namespace Carnation
{
    internal abstract class ColorItemBase : NotifyPropertyBase
    {
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

        private string _contrastRatio;
        public string ContrastRatio
        {
            get => _contrastRatio;
            set => SetProperty(ref _contrastRatio, value);
        }

        protected ColorItemBase(Color foreground, Color background)
        {
            Foreground = foreground;
            Background = background;

            ComputeContrastRatio();

            PropertyChanged += (s, o) =>
            {
                switch (o.PropertyName)
                {
                    case nameof(Foreground):
                    case nameof(Background):
                        ComputeContrastRatio();
                        break;
                }
            };
        }

        private void ComputeContrastRatio()
        {
            ContrastRatio = $"{ColorHelpers.GetContrast(Foreground, Background):N2} : 1";
        }
    }
}
