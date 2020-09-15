using System;
using System.Globalization;
using System.Windows.Data;

namespace Carnation
{
    internal class ContrastRatioConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var contrast = (double)value;
            return $"{contrast:N2}:1";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
