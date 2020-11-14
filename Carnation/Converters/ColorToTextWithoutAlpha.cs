using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Carnation
{
    public class ColorToTextWithoutAlpha : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return $"{color.ToString().Substring(3)}";
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
