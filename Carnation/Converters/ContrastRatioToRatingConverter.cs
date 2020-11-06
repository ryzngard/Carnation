using System;
using System.Globalization;
using System.Windows.Data;

namespace Carnation
{
    internal class ContrastRatioToRatingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var contrast = (double)value;
            return contrast switch
            {
                < 3 => "❌",
                < ContrastHelpers.AA_Contrast => "⚠️",
                < ContrastHelpers.AAA_Contrast => "AA",
                _ => "AAA"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
