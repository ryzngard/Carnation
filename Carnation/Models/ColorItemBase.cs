using System.Windows.Media;

namespace Carnation
{
    public abstract class ColorItemBase : NotifyPropertyBase
    {
        private Color _foreground;
        public Color Foreground
        {
            get => _foreground;
            set => SetProperty(ref _foreground, value);
        }

        private bool _isForegroundEditable = true;
        public bool IsForegroundEditable
        {
            get => _isForegroundEditable;
            set => SetProperty(ref _isForegroundEditable, value);
        }

        private Color _background;
        public Color Background
        {
            get => _background;
            set => SetProperty(ref _background, value);
        }

        private bool _isBackgroundEditable = true;
        public bool IsBackgroundEditable
        {
            get => _isBackgroundEditable;
            set => SetProperty(ref _isBackgroundEditable, value);
        }

        private bool _isBold;
        public bool IsBold
        {
            get => _isBold;
            set => SetProperty(ref _isBold, value);
        }

        private string _contrastRatio;
        public string ContrastRatio
        {
            get => _contrastRatio;
            set => SetProperty(ref _contrastRatio, value);
        }

        private string _contrastRating;
        public string ContrastRating
        {
            get => _contrastRating;
            set => SetProperty(ref _contrastRating, value);
        }

        protected ColorItemBase(Color foreground, Color background, bool isBold)
        {
            _foreground = foreground;
            _background = background;
            _isBold = isBold;

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
            var contrast = ColorHelpers.GetContrast(Foreground, Background);
            ContrastRatio = $"{contrast:N2}";
            ContrastRating = GetContrastSymbol(contrast);
            return;

            string GetContrastSymbol(double contrast)
            {
                if (contrast < 3)
                {
                    return "❌";
                }
                else if (contrast < 4.5)
                {
                    return "⚠️";
                }
                else if (contrast < 7)
                {
                    return "AA";
                }
                else
                {
                    return "AAA";
                }
            }
        }
    }
}
