using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Windows.Media;

namespace Carnation
{
    internal static class ContrastHelpers
    {
        public const double AA_Contrast = 4.5;
        public const double AAA_Contrast = 7.0;

        public static ImmutableArray<ContrastResult> FindSimilarAAColor(
            Color targetColor,
            Color contrastColor)
        {
            return FindSimilarColors(targetColor, contrastColor, AA_Contrast, 150.0);
        }

        public static ImmutableArray<ContrastResult> FindSimilarAAAColor(
            Color targetColor,
            Color contrastColor)
        {
            return FindSimilarColors(targetColor, contrastColor, AAA_Contrast, 150.0);
        }

        public static ImmutableArray<ContrastResult> FindSimilarColors(
            Color targetColor,
            Color contrastColor,
            double minimalContrast,
            double distanceLimit)
        {
            // See if the target color already meets the minimal contrast.
            var contrast = targetColor.GetContrast(contrastColor);
            if (contrast > minimalContrast)
            {
                return ImmutableArray<ContrastResult>.Empty;
            }

            var makeBrighter = targetColor.ToLuminance() > contrastColor.ToLuminance();
            var interval = makeBrighter ? 1 : -1;
            var stopValue = makeBrighter ? 256 : -1;

            double distance;
            var results = new List<ContrastResult>();
            
            for (var r = targetColor.R; r != stopValue; r = (byte)(r + interval))
            {
                distance = ColorHelpers.GetDistance(r, targetColor.G, targetColor.B, targetColor);
                if (distance > distanceLimit)
                {
                    break;
                }

                for (var g = targetColor.G; g != stopValue; g = (byte)(g + interval))
                {
                    distance = ColorHelpers.GetDistance(r, g, targetColor.B, targetColor);
                    if (distance > distanceLimit)
                    {
                        break;
                    }

                    for (var b = targetColor.B; b != stopValue; b = (byte)(b + interval))
                    {
                        distance = ColorHelpers.GetDistance(r, g, b, targetColor);
                        if (distance > distanceLimit)
                        {
                            break;
                        }

                        contrast = ColorHelpers.GetContrast(r, g, b, contrastColor);
                        if (contrast >= minimalContrast)
                        {
                            results.Add(new ContrastResult(Color.FromRgb(r, g, b), contrast, distance));
                        }
                    }
                }
            }

            return results.OrderBy(result => result.Distance).Take(16).ToImmutableArray();
        }


        public class ContrastResult
        {
            public Color Color { get; }
            public double Contrast { get; }
            public double Distance { get; }

            public ContrastResult(Color color, double contrast, double distance)
            {
                Color = color;
                Contrast = contrast;
                Distance = distance;
            }
        }
    }
}
