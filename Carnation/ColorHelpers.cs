using System;
using System.Windows.Media;

namespace Carnation
{
    internal static class ColorHelpers
    {
        // https://www.w3.org/TR/WCAG20/#contrast-ratiodef
        public static double GetContrast(this Color color, Color otherColor)
        {
            var L1 = color.ToLuminance();
            var L2 = otherColor.ToLuminance();
            return L1 > L2
                ? (L1 + 0.05) / (L2 + 0.05)
                : (L2 + 0.05) / (L1 + 0.05);
        }

        // https://www.w3.org/TR/WCAG20/#relativeluminancedef
        public static double ToLuminance(this Color color)
        {
            var R = color.ScR <= 0.03928
                ? color.ScR / 12.92
                : Math.Pow((color.ScR + 0.055) / 1.055, 2.4);

            var G = color.ScG <= 0.03928
                ? color.ScG / 12.92
                : Math.Pow((color.ScG + 0.055) / 1.055, 2.4);

            var B = color.ScB <= 0.03928
                ? color.ScB / 12.92
                : Math.Pow((color.ScB + 0.055) / 1.055, 2.4);

            return (0.2126 * R) + (0.7152 * G) + (0.0722 * B);
        }
    }
}
