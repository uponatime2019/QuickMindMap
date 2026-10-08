using System;
using System.Collections.Generic;
using System.Linq;

namespace QuickMindMap.Models
{
    public class ColorPalette
    {
        public string Name { get; set; } = "Vivid Spectrum";
        public string RootColor { get; set; } = "#2563EB";
        public List<string> BranchColors { get; set; } = new();

        public static readonly List<ColorPalette> AvailablePalettes = new()
        {
            new ColorPalette
            {
                Name = "Vivid Spectrum",
                RootColor = "#1E40AF",
                BranchColors = new List<string>
                {
                    "#2563EB", // Royal Blue
                    "#10B981", // Emerald Green
                    "#F59E0B", // Amber Orange
                    "#EF4444", // Rose Red
                    "#8B5CF6", // Purple
                    "#06B6D4", // Cyan Teal
                    "#EC4899", // Pink
                    "#14B8A6", // Teal
                    "#F97316", // Coral Orange
                    "#6366F1", // Indigo
                    "#84CC16", // Lime
                    "#A855F7"  // Violet
                }
            },
            new ColorPalette
            {
                Name = "Ocean Breeze",
                RootColor = "#0F766E",
                BranchColors = new List<string>
                {
                    "#0284C7", // Sky Blue
                    "#0D9488", // Teal
                    "#2563EB", // Blue
                    "#06B6D4", // Cyan
                    "#3B82F6", // Light Blue
                    "#14B8A6", // Mint Teal
                    "#4F46E5", // Deep Indigo
                    "#0891B2"  // Sea Cyan
                }
            },
            new ColorPalette
            {
                Name = "Forest Bloom",
                RootColor = "#166534",
                BranchColors = new List<string>
                {
                    "#15803D", // Green
                    "#84CC16", // Lime
                    "#059669", // Emerald
                    "#EAB308", // Goldenrod
                    "#10B981", // Mint
                    "#65A30D", // Olive Lime
                    "#D97706", // Amber
                    "#16A34A"  // Leaf Green
                }
            },
            new ColorPalette
            {
                Name = "Sunset Glow",
                RootColor = "#991B1B",
                BranchColors = new List<string>
                {
                    "#DC2626", // Crimson
                    "#EA580C", // Deep Orange
                    "#D97706", // Amber
                    "#CA8A04", // Gold
                    "#E11D48", // Rose
                    "#F59E0B", // Warm Yellow
                    "#BE123C", // Ruby
                    "#FB923C"  // Soft Orange
                }
            },
            new ColorPalette
            {
                Name = "Royal Lavender",
                RootColor = "#581C87",
                BranchColors = new List<string>
                {
                    "#7C3AED", // Violet
                    "#9333EA", // Purple
                    "#C026D3", // Fuchsia
                    "#4F46E5", // Indigo
                    "#A855F7", // Lavender
                    "#DB2777", // Deep Pink
                    "#6366F1", // Periwinkle
                    "#8B5CF6"  // Lilac
                }
            },
            new ColorPalette
            {
                Name = "Nordic Slate",
                RootColor = "#334155",
                BranchColors = new List<string>
                {
                    "#475569", // Slate
                    "#2563EB", // Steel Blue
                    "#059669", // Dark Sage
                    "#64748B", // Cool Gray
                    "#0284C7", // Nordic Blue
                    "#4B5563", // Zinc
                    "#0D9488", // Ice Teal
                    "#374151"  // Iron
                }
            },
            new ColorPalette
            {
                Name = "Candy Pastel",
                RootColor = "#6366F1",
                BranchColors = new List<string>
                {
                    "#38BDF8", // Pastel Sky
                    "#34D399", // Pastel Emerald
                    "#FBBF24", // Pastel Amber
                    "#F87171", // Pastel Red
                    "#A78BFA", // Pastel Purple
                    "#F472B6", // Pastel Pink
                    "#2DD4BF", // Pastel Teal
                    "#FB923C"  // Pastel Orange
                }
            }
        };

        public static ColorPalette GetPalette(string? name)
        {
            if (string.IsNullOrEmpty(name)) return AvailablePalettes[0];
            return AvailablePalettes.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? AvailablePalettes[0];
        }

        public static string GetColorForBranchIndex(int index, string? paletteName = null)
        {
            var palette = GetPalette(paletteName);
            if (palette.BranchColors.Count == 0) return "#2563EB";
            return palette.BranchColors[Math.Abs(index) % palette.BranchColors.Count];
        }
    }
}
