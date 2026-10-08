using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace QuickMindMap.Models
{
    public class MindMapMetadata
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("title")]
        public string Title { get; set; } = "Untitled Map";

        [JsonProperty("group")]
        public string Group { get; set; } = "General";

        [JsonProperty("createdAt")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

        [JsonProperty("modifiedAt")]
        public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.Now;

        [JsonProperty("nodeCount")]
        public int NodeCount { get; set; } = 1;

        [JsonProperty("accentColor")]
        public string AccentColor { get; set; } = "#2563EB";

        [JsonIgnore]
        public Microsoft.UI.Xaml.Media.SolidColorBrush AccentBrush => Helpers.MapColorHelper.HexToBrush(AccentColor);

        [JsonProperty("filePath")]
        public string FilePath { get; set; } = string.Empty;

        [JsonProperty("previewTopics")]
        public List<string> PreviewTopics { get; set; } = new();

        [JsonIgnore]
        public string FirstLetter
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Title)) return "#";
                char c = char.ToUpperInvariant(Title.TrimStart()[0]);
                return char.IsLetter(c) ? c.ToString() : "#";
            }
        }

        [JsonIgnore]
        public string FormattedModifiedDate
        {
            get
            {
                var diff = DateTimeOffset.Now - ModifiedAt;
                if (diff.TotalMinutes < 1) return "Just now";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
                if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
                return ModifiedAt.ToString("MMM d, yyyy");
            }
        }
    }
}
