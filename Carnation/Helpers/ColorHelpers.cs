using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Windows.Media;

namespace Carnation
{
    internal static class ColorHelpers
    {
        private static readonly ImmutableDictionary<byte, float> _scRgbLookup = Enumerable.Range(0, 256)
            .ToImmutableDictionary(b => (byte)b, b => sRgbToScRgb((byte)b));

        // https://www.w3.org/TR/WCAG20/#contrast-ratiodef
        public static double GetContrast(this Color color, Color otherColor)
        {
            var L1 = color.ToLuminance();
            var L2 = otherColor.ToLuminance();
            return GetContrast(L1, L2);
        }

        // https://www.w3.org/TR/WCAG20/#contrast-ratiodef
        public static double GetContrast(byte r, byte g, byte b, Color otherColor)
        {
            var L1 = GetLuminance(r, g, b);
            var L2 = otherColor.ToLuminance();
            return GetContrast(L1, L2);
        }

        public static double GetContrast(double luminance1, double luminance2)
        {
            return luminance1 > luminance2
                ? (luminance1 + 0.05) / (luminance2 + 0.05)
                : (luminance2 + 0.05) / (luminance1 + 0.05);
        }

        // https://www.w3.org/TR/WCAG20/#relativeluminancedef
        public static double ToLuminance(this Color color)
        {
            return GetLuminance(color.ScR, color.ScG, color.ScB);
        }

        public static double GetLuminance(byte r, byte g, byte b)
        {
            var scR = _scRgbLookup[r];
            var scG = _scRgbLookup[g];
            var scB = _scRgbLookup[b];

            return GetLuminance(scR, scG, scB);
        }

        // https://www.w3.org/TR/WCAG20/#relativeluminancedef
        public static double GetLuminance(float scR, float scG, float scB)
        {
            var R = scR <= 0.03928
                ? scR / 12.92
                : Math.Pow((scR + 0.055) / 1.055, 2.4);

            var G = scG <= 0.03928
                ? scG / 12.92
                : Math.Pow((scG + 0.055) / 1.055, 2.4);

            var B = scB <= 0.03928
                ? scB / 12.92
                : Math.Pow((scB + 0.055) / 1.055, 2.4);

            return (0.2126 * R) + (0.7152 * G) + (0.0722 * B);
        }

        private static float sRgbToScRgb(byte bval)
        {
            var val = bval / 255.0f;

            if (!(val > 0.0))       // Handles NaN case too. (Though, NaN isn't actually
                                    // possible in this case.)
            {
                return (0.0f);
            }
            else if (val <= 0.04045)
            {
                return (val / 12.92f);
            }
            else if (val < 1.0f)
            {
                return (float)Math.Pow(((double)val + 0.055) / 1.055, 2.4);
            }
            else
            {
                return (1.0f);
            }
        }

        // https://www.compuphase.com/cmetric.htm
        public static double GetDistance(this Color color, Color otherColor)
        {
            var rmean = (color.R + (long)otherColor.R) / 2;
            var dR = color.R - (long)otherColor.R;
            var dG = color.G - (long)otherColor.G;
            var dB = color.B - (long)otherColor.B;
            return Math.Sqrt((((512 + rmean) * dR * dR) >> 8) + (4 * dG * dG) + (((767 - rmean) * dB * dB) >> 8));
        }

        // https://www.compuphase.com/cmetric.htm
        public static double GetDistance(byte r, byte g, byte b, Color otherColor)
        {
            var rmean = (r + (long)otherColor.R) / 2;
            var dR = r - (long)otherColor.R;
            var dG = g - (long)otherColor.G;
            var dB = b - (long)otherColor.B;
            return Math.Sqrt((((512 + rmean) * dR * dR) >> 8) + (4 * dG * dG) + (((767 - rmean) * dB * dB) >> 8));
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
                var k = (d + (hue / 60)) % 6;
                var v = value - (value * saturation * Math.Max(Math.Min(Math.Min(k, 4 - k), 1), 0));
                return (byte)Math.Round(v * 255);
            }

            return Color.FromRgb(f(5), f(3), f(1));
        }

        internal static Color ToColor(uint argb)
        {
            return Color.FromArgb((byte)((argb & -16777216) >> 0x18),
                              (byte)((argb & 0xff0000) >> 0x10),
                              (byte)((argb & 0xff00) >> 8),
                              (byte)(argb & 0xff));
        }

        internal static int ToInt(Color color)
        {
            return (color.A << 0x18) +
                (color.R << 0x10) +
                (color.G << 0x08) +
                color.B;
        }

        internal static double GetHue(Color color)
            => System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B).GetHue();

        internal static double GetBrightness(Color color)
            => System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B).GetBrightness();

        internal static double GetSaturation(Color color)
            => System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B).GetSaturation();

        private static double Clamp(double value, double min, double max)
            => Math.Max(min, Math.Min(value, max));

        internal static bool TryGetSystemDrawingName(Color color, out string name)
        {
            foreach (var knownColorKey in Enum.GetValues(typeof(System.Drawing.KnownColor)).Cast<System.Drawing.KnownColor>())
            {
                var knownColor = System.Drawing.Color.FromKnownColor(knownColorKey);
                if (knownColor.R == color.R &&
                    knownColor.G == color.G &&
                    knownColor.B == color.B)
                // Alpha doesn't seem helpful here, since it doesn't really dictate what the color is but
                // rather the transparency of the color
                {
                    name = Enum.GetName(typeof(System.Drawing.KnownColor), knownColorKey);
                    return true;
                }
            }

            name = null;
            return false;
        }

        internal static bool TryGetWindowsMediaName(Color color, out string name)
        {
            var colorNames = GetColorNames();
            var colorNamePair = colorNames.FirstOrDefault(pair => color == pair.Item1);

            if (string.IsNullOrEmpty(colorNamePair.Item2))
            {
                name = null;
                return false;
            }

            name = colorNamePair.Item2;
            return true;
        }

        private static ImmutableArray<(Color, string)> _colorNames;
        private static ImmutableArray<(Color, string)> GetColorNames()
        {
            if (_colorNames.IsDefault)
            {
                var properties = typeof(Colors).GetProperties(BindingFlags.Public | BindingFlags.Static);
                _colorNames = properties
                    .Where(p => p.PropertyType == typeof(Color))
                    .Select(p => ((Color)p.GetValue(null), p.Name))
                    .ToImmutableArray();
            }

            return _colorNames;
        }
    }
}
