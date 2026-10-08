using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace QuickMindMap.Models
{
    public class MindMapNode
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        [JsonProperty("parentId")]
        public string? ParentId { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; } = "New Topic";

        [JsonProperty("note")]
        public string? Note { get; set; }

        [JsonProperty("isRoot")]
        public bool IsRoot { get; set; }

        [JsonProperty("branchColor")]
        public string BranchColor { get; set; } = "#2563EB";

        [JsonProperty("fillColor")]
        public string? FillColor { get; set; }

        [JsonProperty("textColor")]
        public string? TextColor { get; set; }

        [JsonProperty("fontSize")]
        public double FontSize { get; set; } = 14;

        [JsonProperty("fontWeight")]
        public string FontWeight { get; set; } = "Normal"; // "Normal", "SemiBold", "Bold"

        [JsonProperty("fontStyle")]
        public string FontStyle { get; set; } = "Normal"; // "Normal", "Italic"

        [JsonProperty("isStrikethrough")]
        public bool IsStrikethrough { get; set; }

        [JsonProperty("shapeType")]
        public string ShapeType { get; set; } = "RoundedRectangle"; // "RoundedRectangle", "Pill", "Rectangle", "Ellipse", "Underline", "Bubble"

        [JsonProperty("icon")]
        public string? Icon { get; set; }

        [JsonProperty("progress")]
        public int? Progress { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonProperty("isCollapsed")]
        public bool IsCollapsed { get; set; }

        [JsonProperty("side")]
        public string Side { get; set; } = "Auto"; // "Left", "Right", "Auto"

        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }

        [JsonProperty("width")]
        public double Width { get; set; } = 120;

        [JsonProperty("height")]
        public double Height { get; set; } = 40;

        [JsonProperty("children")]
        public List<MindMapNode> Children { get; set; } = new();

        [JsonIgnore]
        public int TotalDescendantsCount
        {
            get
            {
                int count = Children.Count;
                foreach (var child in Children)
                {
                    count += child.TotalDescendantsCount;
                }
                return count;
            }
        }

        [JsonIgnore]
        public bool HasChildren => Children.Count > 0;

        public MindMapNode CloneSubtree(string? newParentId = null)
        {
            var clone = new MindMapNode
            {
                Id = Guid.NewGuid().ToString("N"),
                ParentId = newParentId,
                Text = this.Text,
                Note = this.Note,
                IsRoot = false,
                BranchColor = this.BranchColor,
                FillColor = this.FillColor,
                TextColor = this.TextColor,
                FontSize = this.FontSize,
                FontWeight = this.FontWeight,
                FontStyle = this.FontStyle,
                IsStrikethrough = this.IsStrikethrough,
                ShapeType = this.ShapeType,
                Icon = this.Icon,
                Progress = this.Progress,
                Tags = new List<string>(this.Tags),
                IsCollapsed = this.IsCollapsed,
                Side = this.Side,
                X = this.X,
                Y = this.Y,
                Width = this.Width,
                Height = this.Height,
                Children = new List<MindMapNode>()
            };

            foreach (var child in this.Children)
            {
                clone.Children.Add(child.CloneSubtree(clone.Id));
            }

            return clone;
        }

        public MindMapNode? FindNode(string id)
        {
            if (Id == id) return this;
            foreach (var child in Children)
            {
                var found = child.FindNode(id);
                if (found != null) return found;
            }
            return null;
        }

        public MindMapNode? FindParent(string childId)
        {
            foreach (var child in Children)
            {
                if (child.Id == childId) return this;
                var found = child.FindParent(childId);
                if (found != null) return found;
            }
            return null;
        }

        public List<MindMapNode> GetAllNodes()
        {
            var list = new List<MindMapNode> { this };
            foreach (var child in Children)
            {
                list.AddRange(child.GetAllNodes());
            }
            return list;
        }

        public int GetDepth(MindMapNode root)
        {
            if (root.Id == this.Id) return 0;
            return CalculateDepthRecursive(root, this.Id, 0);
        }

        private static int CalculateDepthRecursive(MindMapNode current, string targetId, int currentDepth)
        {
            if (current.Id == targetId) return currentDepth;
            foreach (var child in current.Children)
            {
                int depth = CalculateDepthRecursive(child, targetId, currentDepth + 1);
                if (depth != -1) return depth;
            }
            return -1;
        }
    }
}
