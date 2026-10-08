using System;
using System.Collections.Generic;
using System.Linq;
using QuickMindMap.Helpers;
using QuickMindMap.Models;

namespace QuickMindMap.Services
{
    public class ConnectorLine
    {
        public string Id { get; set; } = string.Empty;
        public string FromNodeId { get; set; } = string.Empty;
        public string ToNodeId { get; set; } = string.Empty;
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
        public double Control1X { get; set; }
        public double Control1Y { get; set; }
        public double Control2X { get; set; }
        public double Control2Y { get; set; }
        public string ColorHex { get; set; } = "#2563EB";
        public double Thickness { get; set; } = 2.5;
        public string SvgPathData { get; set; } = string.Empty;
        public string LineStyle { get; set; } = "BezierCurved";
    }

    public class LayoutEngine
    {
        private const double BaseHorizontalGap = 80.0;
        private const double BaseVerticalGap = 16.0;

        public static void CalculateNodeDimensions(MindMapNode node, double scaleRatio = 0)
        {
            if (scaleRatio <= 0)
            {
                scaleRatio = DisplayScaleHelper.CurrentScaleRatio;
            }
            if (scaleRatio < 1.0)
            {
                scaleRatio = 1.0;
            }

            // Estimate comfortable dimensions based on text, font size, icon, and badges
            double charWidth = node.FontSize * 0.65;
            string text = string.IsNullOrWhiteSpace(node.Text) ? " " : node.Text;
            double estimatedTextWidth = text.Length * charWidth;
            double padding = 36.0;

            if (!string.IsNullOrEmpty(node.Icon))
            {
                padding += 24.0;
            }
            if (node.Progress.HasValue)
            {
                padding += 20.0;
            }
            if (!string.IsNullOrEmpty(node.Note))
            {
                padding += 18.0;
            }

            double minWidth = (node.IsRoot ? 140.0 : 110.0) * scaleRatio;
            double maxWidth = 380.0 * scaleRatio;
            double baseHeight = node.IsRoot ? 48.0 : (node.FontSize > 14 ? 42.0 : 38.0);

            node.Width = Math.Clamp((estimatedTextWidth + padding) * scaleRatio, minWidth, maxWidth);
            node.Height = baseHeight * scaleRatio;

            foreach (var child in node.Children)
            {
                CalculateNodeDimensions(child, scaleRatio);
            }
        }

        public static void ApplyAutoLayout(MindMapDocument document, double scaleRatio = 0)
        {
            if (document.RootNode == null) return;

            if (scaleRatio <= 0)
            {
                scaleRatio = DisplayScaleHelper.CurrentScaleRatio;
            }
            if (scaleRatio < 1.0)
            {
                scaleRatio = 1.0;
            }

            var root = document.RootNode;
            CalculateNodeDimensions(root, scaleRatio);

            string layout = document.LayoutType ?? "MindMap";
            string lineStyle = document.LineStyle ?? "BezierCurved";

            if (layout == "Freeform")
            {
                // In freeform mode, keep current X/Y coordinates
                return;
            }

            // Central root position
            root.X = 0;
            root.Y = 0;

            var visibleChildren = root.Children;
            if (visibleChildren.Count == 0) return;

            double hGap = BaseHorizontalGap * scaleRatio;
            double vGap = BaseVerticalGap * scaleRatio;

            if (layout == "RightOnly")
            {
                foreach (var child in visibleChildren) child.Side = "Right";
                LayoutSubtreeList(visibleChildren, root.X + root.Width / 2 + hGap, root.Y, "Right", lineStyle, hGap, vGap);
            }
            else if (layout == "LeftOnly")
            {
                foreach (var child in visibleChildren) child.Side = "Left";
                LayoutSubtreeList(visibleChildren, root.X - root.Width / 2 - hGap, root.Y, "Left", lineStyle, hGap, vGap);
            }
            else // "MindMap" (Dual-sided balanced)
            {
                var rightChildren = new List<MindMapNode>();
                var leftChildren = new List<MindMapNode>();

                // Partition children based on explicit Side or alternate evenly
                int autoIndex = 0;
                foreach (var child in visibleChildren)
                {
                    if (child.Side == "Left")
                    {
                        leftChildren.Add(child);
                    }
                    else if (child.Side == "Right")
                    {
                        rightChildren.Add(child);
                    }
                    else
                    {
                        if (autoIndex % 2 == 0)
                        {
                            child.Side = "Right";
                            rightChildren.Add(child);
                        }
                        else
                        {
                            child.Side = "Left";
                            leftChildren.Add(child);
                        }
                        autoIndex++;
                    }
                }

                // If all children were assigned to one side unintentionally, balance them
                if (leftChildren.Count == 0 && rightChildren.Count > 1)
                {
                    int half = (rightChildren.Count + 1) / 2;
                    for (int i = half; i < rightChildren.Count; i++)
                    {
                        rightChildren[i].Side = "Left";
                        leftChildren.Add(rightChildren[i]);
                    }
                    rightChildren.RemoveRange(half, rightChildren.Count - half);
                }
                else if (rightChildren.Count == 0 && leftChildren.Count > 1)
                {
                    int half = (leftChildren.Count + 1) / 2;
                    for (int i = half; i < leftChildren.Count; i++)
                    {
                        leftChildren[i].Side = "Right";
                        rightChildren.Add(leftChildren[i]);
                    }
                    leftChildren.RemoveRange(half, leftChildren.Count - half);
                }

                if (rightChildren.Count > 0)
                {
                    LayoutSubtreeList(rightChildren, root.X + root.Width / 2 + hGap, root.Y, "Right", lineStyle, hGap, vGap);
                }
                if (leftChildren.Count > 0)
                {
                    LayoutSubtreeList(leftChildren, root.X - root.Width / 2 - hGap, root.Y, "Left", lineStyle, hGap, vGap);
                }
            }
        }

        private static void LayoutSubtreeList(List<MindMapNode> nodes, double targetX, double centerY, string side, string lineStyle, double hGap, double vGap)
        {
            if (nodes.Count == 0) return;

            // Compute total height needed by all subtrees
            double totalHeight = 0;
            var subtreeHeights = new List<double>();

            foreach (var node in nodes)
            {
                double h = CalculateSubtreeHeight(node, vGap);
                subtreeHeights.Add(h);
                totalHeight += h;
            }
            totalHeight += (nodes.Count - 1) * vGap;

            double currentY = centerY - totalHeight / 2.0;

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                node.Side = side;
                double subHeight = subtreeHeights[i];
                double nodeCenterY = currentY + subHeight / 2.0;

                if (side == "Right")
                {
                    node.X = targetX + node.Width / 2.0;
                }
                else
                {
                    node.X = targetX - node.Width / 2.0;
                }
                node.Y = nodeCenterY;

                currentY += subHeight + vGap;

                // Layout children if not collapsed
                if (!node.IsCollapsed && node.Children.Count > 0)
                {
                    double nextTargetX = (side == "Right")
                        ? node.X + node.Width / 2.0 + hGap
                        : node.X - node.Width / 2.0 - hGap;

                    LayoutSubtreeList(node.Children, nextTargetX, node.Y, side, lineStyle, hGap, vGap);
                }
            }
        }

        private static double CalculateSubtreeHeight(MindMapNode node, double vGap)
        {
            if (node.IsCollapsed || node.Children.Count == 0)
            {
                return node.Height;
            }

            double childrenHeight = 0;
            foreach (var child in node.Children)
            {
                childrenHeight += CalculateSubtreeHeight(child, vGap);
            }
            childrenHeight += (node.Children.Count - 1) * vGap;

            return Math.Max(node.Height, childrenHeight);
        }

        public static List<ConnectorLine> GenerateConnectors(MindMapDocument document)
        {
            var list = new List<ConnectorLine>();
            if (document.RootNode == null) return list;

            string lineStyle = document.LineStyle ?? "BezierCurved";
            GenerateConnectorsRecursive(document.RootNode, lineStyle, list);
            return list;
        }

        private static void GenerateConnectorsRecursive(MindMapNode parent, string lineStyle, List<ConnectorLine> list)
        {
            if (parent.IsCollapsed) return;

            foreach (var child in parent.Children)
            {
                bool isRight = child.X >= parent.X;

                double startX = isRight ? (parent.X + parent.Width / 2.0) : (parent.X - parent.Width / 2.0);
                double startY = parent.Y;

                double endX = isRight ? (child.X - child.Width / 2.0) : (child.X + child.Width / 2.0);
                double endY = child.Y;

                double dx = Math.Abs(endX - startX);
                double controlOffset = Math.Max(dx * 0.5, 30.0);

                double c1x = isRight ? (startX + controlOffset) : (startX - controlOffset);
                double c1y = startY;
                double c2x = isRight ? (endX - controlOffset) : (endX + controlOffset);
                double c2y = endY;

                string pathData;
                if (lineStyle == "StraightLine")
                {
                    pathData = $"M {startX:F1},{startY:F1} L {endX:F1},{endY:F1}";
                }
                else if (lineStyle == "RoundedStep")
                {
                    double midX = (startX + endX) / 2.0;
                    pathData = $"M {startX:F1},{startY:F1} L {midX:F1},{startY:F1} L {midX:F1},{endY:F1} L {endX:F1},{endY:F1}";
                }
                else // "BezierCurved"
                {
                    pathData = $"M {startX:F1},{startY:F1} C {c1x:F1},{c1y:F1} {c2x:F1},{c2y:F1} {endX:F1},{endY:F1}";
                }

                double thickness = parent.IsRoot ? 3.0 : 2.0;
                string color = !string.IsNullOrEmpty(child.BranchColor) ? child.BranchColor : parent.BranchColor;

                list.Add(new ConnectorLine
                {
                    Id = $"{parent.Id}_{child.Id}",
                    FromNodeId = parent.Id,
                    ToNodeId = child.Id,
                    StartX = startX,
                    StartY = startY,
                    EndX = endX,
                    EndY = endY,
                    Control1X = c1x,
                    Control1Y = c1y,
                    Control2X = c2x,
                    Control2Y = c2y,
                    ColorHex = color,
                    Thickness = thickness,
                    SvgPathData = pathData,
                    LineStyle = lineStyle
                });

                GenerateConnectorsRecursive(child, lineStyle, list);
            }
        }

        public static (double minX, double minY, double maxX, double maxY) GetBoundingBox(MindMapDocument doc)
        {
            if (doc.RootNode == null) return (-200, -200, 200, 200);

            var nodes = doc.RootNode.GetAllNodes().Where(n => !IsNodeHidden(doc.RootNode, n)).ToList();
            if (nodes.Count == 0) return (-200, -200, 200, 200);

            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            foreach (var node in nodes)
            {
                double left = node.X - node.Width / 2.0;
                double top = node.Y - node.Height / 2.0;
                double right = node.X + node.Width / 2.0;
                double bottom = node.Y + node.Height / 2.0;

                minX = Math.Min(minX, left);
                minY = Math.Min(minY, top);
                maxX = Math.Max(maxX, right);
                maxY = Math.Max(maxY, bottom);
            }

            return (minX, minY, maxX, maxY);
        }

        private static bool IsNodeHidden(MindMapNode root, MindMapNode target)
        {
            if (root.Id == target.Id) return false;
            return CheckHiddenRecursive(root, target.Id);
        }

        private static bool CheckHiddenRecursive(MindMapNode current, string targetId)
        {
            if (current.IsCollapsed)
            {
                if (current.Children.Any(c => c.FindNode(targetId) != null))
                {
                    return true;
                }
            }

            foreach (var child in current.Children)
            {
                if (CheckHiddenRecursive(child, targetId)) return true;
            }
            return false;
        }
    }
}
