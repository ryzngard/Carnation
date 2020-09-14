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

        /// <summary>
        /// From https://en.wikipedia.org/wiki/HSL_and_HSV
        /// </summary> 
        internal static Color HSVToColor(double hue, double saturation, double value)
        {
            hue = Clamp(hue, 0, 360);
            saturation = Clamp(saturation, 0, 1);
            value = Clamp(value, 0, 1);

            byte f(double d)
            {
                var k = (d + hue / 60) % 6;
                var v = value - value * saturation * Math.Max(Math.Min(Math.Min(k, 4 - k), 1), 0);
                return (byte)Math.Round(v * 255);
            }

            return Color.FromRgb(f(5), f(3), f(1));
        }

        internal static double GetHue(Color color)
            => System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B).GetHue();

        internal static double GetBrightness(Color color)
            => System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B).GetBrightness();

        internal static double GetSaturation(Color color)
            => System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B).GetSaturation();

        private static double Clamp(double value, double min, double max)
            => Math.Max(min, Math.Min(value, max));
    }
}
