using System;
using System.Globalization;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace QuickMindMap.Helpers
{
    public static class MapColorHelper
    {
        public static Brush GetSystemAccentBrush()
        {
            if (Application.Current?.Resources != null)
            {
                if (Application.Current.Resources.TryGetValue("AccentFillColorDefaultBrush", out var brush) && brush is Brush b)
                    return b;
                if (Application.Current.Resources.TryGetValue("SystemControlHighlightAccentBrush", out var legacyBrush) && legacyBrush is Brush lb)
                    return lb;
                if (Application.Current.Resources.TryGetValue("SystemAccentColor", out var color) && color is Color c)
                    return new SolidColorBrush(c);
            }
            return new SolidColorBrush(Color.FromArgb(255, 0, 120, 215));
        }

        public static Color HexToColor(string? hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return Color.FromArgb(255, 37, 99, 235);

            hex = hex.Trim().TrimStart('#');
            if (hex.Length == 6)
            {
                if (byte.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r) &&
                    byte.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g) &&
                    byte.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
                {
                    return Color.FromArgb(255, r, g, b);
                }
            }
            else if (hex.Length == 8)
            {
                if (byte.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte a) &&
                    byte.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r) &&
                    byte.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g) &&
                    byte.TryParse(hex.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
                {
                    return Color.FromArgb(a, r, g, b);
                }
            }

            return Color.FromArgb(255, 37, 99, 235);
        }

        public static SolidColorBrush HexToBrush(string? hex, double opacity = 1.0)
        {
            var color = HexToColor(hex);
            if (opacity < 1.0)
            {
                color = Color.FromArgb((byte)(color.A * opacity), color.R, color.G, color.B);
            }
            return new SolidColorBrush(color);
        }

        public static SolidColorBrush GetTintBrush(string? hex, double tintAmount = 0.12)
        {
            var color = HexToColor(hex);
            return new SolidColorBrush(Color.FromArgb((byte)(255 * tintAmount), color.R, color.G, color.B));
        }

        public static string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}
