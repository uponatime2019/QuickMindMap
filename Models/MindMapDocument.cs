using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace QuickMindMap.Models
{
    public class MindMapDocument
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        [JsonProperty("title")]
        public string Title { get; set; } = "Main Topic";

        [JsonProperty("group")]
        public string Group { get; set; } = "General";

        [JsonProperty("createdAt")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

        [JsonProperty("modifiedAt")]
        public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.Now;

        [JsonProperty("themePalette")]
        public string ThemePalette { get; set; } = "Vivid Spectrum";

        [JsonProperty("layoutType")]
        public string LayoutType { get; set; } = "MindMap"; // "MindMap", "RightOnly", "LeftOnly", "Freeform"

        [JsonProperty("lineStyle")]
        public string LineStyle { get; set; } = "BezierCurved"; // "BezierCurved", "RoundedStep", "StraightLine"

        [JsonProperty("canvasBackground")]
        public string CanvasBackground { get; set; } = "GridDots"; // "Default", "PureWhite", "GridDots", "DarkSlate"

        [JsonProperty("zoomLevel")]
        public double ZoomLevel { get; set; } = 1.0;

        [JsonProperty("panOffsetX")]
        public double PanOffsetX { get; set; } = 0.0;

        [JsonProperty("panOffsetY")]
        public double PanOffsetY { get; set; } = 0.0;

        [JsonIgnore]
        public string? FilePath { get; set; }

        [JsonProperty("rootNode")]
        public MindMapNode RootNode { get; set; } = new();

        public static MindMapDocument CreateDefault(string title = "Main Topic", string group = "General", string palette = "Vivid Spectrum")
        {
            var pal = ColorPalette.GetPalette(palette);
            var doc = new MindMapDocument
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = title,
                Group = string.IsNullOrWhiteSpace(group) ? "General" : group,
                CreatedAt = DateTimeOffset.Now,
                ModifiedAt = DateTimeOffset.Now,
                ThemePalette = palette,
                LayoutType = "MindMap",
                LineStyle = "BezierCurved",
                CanvasBackground = "GridDots",
                ZoomLevel = 1.0,
                PanOffsetX = 0,
                PanOffsetY = 0,
                RootNode = new MindMapNode
                {
                    Id = Guid.NewGuid().ToString("N"),
                    ParentId = null,
                    Text = title,
                    IsRoot = true,
                    BranchColor = pal.RootColor,
                    FillColor = pal.RootColor,
                    TextColor = "#FFFFFF",
                    FontSize = 18,
                    FontWeight = "Bold",
                    ShapeType = "Pill",
                    Width = 150,
                    Height = 46,
                    Children = new List<MindMapNode>()
                }
            };

            return doc;
        }

        public int GetNodeCount()
        {
            return RootNode != null ? 1 + RootNode.TotalDescendantsCount : 0;
        }

        public MindMapMetadata ToMetadata(string filePath = "")
        {
            var preview = new List<string>();
            if (RootNode != null && RootNode.Children != null)
            {
                foreach (var child in RootNode.Children.Take(4))
                {
                    if (!string.IsNullOrWhiteSpace(child.Text))
                    {
                        preview.Add(child.Text);
                    }
                }
            }

            var pal = ColorPalette.GetPalette(ThemePalette);

            return new MindMapMetadata
            {
                Id = this.Id,
                Title = this.Title,
                Group = this.Group,
                CreatedAt = this.CreatedAt,
                ModifiedAt = this.ModifiedAt,
                NodeCount = GetNodeCount(),
                AccentColor = RootNode?.BranchColor ?? pal.RootColor,
                FilePath = filePath,
                PreviewTopics = preview
            };
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public static MindMapDocument? FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonConvert.DeserializeObject<MindMapDocument>(json);
        }

        public MindMapDocument Clone()
        {
            string json = ToJson();
            var copy = FromJson(json) ?? CreateDefault();
            copy.Id = Guid.NewGuid().ToString("N");
            copy.FilePath = null;
            copy.Title = $"{this.Title} (Copy)";
            copy.CreatedAt = DateTimeOffset.Now;
            copy.ModifiedAt = DateTimeOffset.Now;
            if (copy.RootNode != null)
            {
                copy.RootNode = copy.RootNode.CloneSubtree(null);
                copy.RootNode.IsRoot = true;
                copy.RootNode.Text = copy.Title;
            }
            return copy;
        }
    }
}
