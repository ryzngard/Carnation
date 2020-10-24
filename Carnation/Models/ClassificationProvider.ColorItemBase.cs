using System.Diagnostics.Contracts;
using System.Windows.Media;

namespace Carnation
{
    internal partial class ClassificationProvider
    {
        internal abstract class ColorItemBase : NotifyPropertyBase
        {
            private uint _foregroundColorRef;
            public uint ForegroundColorRef
            {
                get => _foregroundColorRef;
                set
                {
                    _foregroundColorRef = value;
                    NotifyPropertyChanged(nameof(Foreground));
                }
            }

            public Color Foreground
            {
                get => IsForegroundEditable
                    ? FontsAndColorsHelper.TryGetColor(ForegroundColorRef) ?? PlainTextForeground
                    : Colors.Transparent;
                set
                {
                    Contract.Assert(IsUpdating || IsForegroundEditable);
                    ForegroundColorRef = FontsAndColorsHelper.GetColorRef(value, PlainTextForeground);
                }
            }

            private bool _isForegroundEditable = true;
            public bool IsForegroundEditable
            {
                get => _isForegroundEditable;
                set => SetProperty(ref _isForegroundEditable, value);
            }

            private uint _backgroundColorRef;
            public uint BackgroundColorRef
            {
                get => _backgroundColorRef;
                set
                {
                    _backgroundColorRef = value;
                    NotifyPropertyChanged(nameof(Background));
                }
            }

            public Color Background
            {
                get => IsBackgroundEditable
                    ? FontsAndColorsHelper.TryGetColor(BackgroundColorRef) ?? PlainTextBackground
                    : Colors.Transparent;
                set
                {
                    Contract.Assert(IsUpdating || IsBackgroundEditable);
                    BackgroundColorRef = FontsAndColorsHelper.GetColorRef(value, PlainTextBackground);
                }
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
                set
                {
                    Contract.Assert(IsUpdating || IsBoldEditable);
                    SetProperty(ref _isBold, value);
                }
            }

            private bool _isBoldEditable = true;
            public bool IsBoldEditable
            {
                get => _isBoldEditable;
                set => SetProperty(ref _isBoldEditable, value);
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

            protected ColorItemBase(
                uint foregroundColorRef,
                uint backgroundColorRef,
                bool isBold,
                bool isForegroundEditable,
                bool isBackgroundEditable,
                bool isBoldEditable)
            {
                _foregroundColorRef = foregroundColorRef;
                _backgroundColorRef = backgroundColorRef;
                _isBold = isBold;
                _isForegroundEditable = isForegroundEditable;
                _isBackgroundEditable = isBackgroundEditable;
                _isBoldEditable = isBoldEditable;

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
                if (!IsForegroundEditable || !IsBackgroundEditable)
                {
                    ContrastRatio = "N/A";
                    ContrastRating = "N/A";
                }

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
}
