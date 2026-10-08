using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using QuickMindMap.Helpers;
using QuickMindMap.Models;
using QuickMindMap.Services;

namespace QuickMindMap.ViewModels
{
    public class EditorViewModel : ViewModelBase
    {
        private MindMapDocument _document;
        private MindMapNode? _selectedNode;
        private ObservableCollection<ConnectorLine> _connectors = new();
        private readonly UndoRedoManager _undoRedo = new();
        private readonly DispatcherQueueTimer _autoSaveTimer;

        private double _zoomLevel = 1.0;
        private double _panX = 0;
        private double _panY = 0;
        private string _saveStatusText = "Saved";
        private bool _isDirty;
        private bool _isFullscreen;
        private bool _isEditingText;

        public MindMapDocument Document
        {
            get => _document;
            set
            {
                if (SetProperty(ref _document, value))
                {
                    _undoRedo.Clear();
                    _undoRedo.RecordState(_document);
                    SelectedNode = _document.RootNode;
                    bool needsLayout = _document != null && (_document.LayoutType != "Freeform" || (_document.RootNode?.Children.Any(c => c.X == 0 && c.Y == 0) ?? false));
                    RefreshLayoutAndConnectors(needsLayout);
                    OnPropertyChanged(nameof(DocumentTitle));
                    OnPropertyChanged(nameof(PaletteName));
                    OnPropertyChanged(nameof(LayoutType));
                    OnPropertyChanged(nameof(LineStyle));
                }
            }
        }

        public string DocumentTitle
        {
            get => _document?.Title ?? "Untitled";
            set
            {
                if (_document != null && _document.Title != value)
                {
                    RecordUndo("Change Title");
                    _document.Title = value;
                    if (_document.RootNode != null && _document.RootNode.Text == "Main Topic")
                    {
                        _document.RootNode.Text = value;
                    }
                    MarkDirty();
                    OnPropertyChanged();
                }
            }
        }

        public MindMapNode? SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (SetProperty(ref _selectedNode, value))
                {
                    OnPropertyChanged(nameof(HasSelectedNode));
                    OnPropertyChanged(nameof(CanAddSibling));
                    OnPropertyChanged(nameof(CanDeleteNode));
                    OnPropertyChanged(nameof(SelectedNodeText));
                    OnPropertyChanged(nameof(SelectedNodeNote));
                    OnPropertyChanged(nameof(SelectedNodeFontSize));
                    OnPropertyChanged(nameof(SelectedNodeIsBold));
                    OnPropertyChanged(nameof(SelectedNodeIsItalic));
                    OnPropertyChanged(nameof(SelectedNodeIsStrikethrough));
                    OnPropertyChanged(nameof(SelectedNodeColor));
                    OnPropertyChanged(nameof(SelectedNodeShape));
                    OnPropertyChanged(nameof(SelectedNodeIcon));
                }
            }
        }

        public bool HasSelectedNode => SelectedNode != null;
        public bool CanAddSibling => SelectedNode != null && !SelectedNode.IsRoot;
        public bool CanDeleteNode => SelectedNode != null && !SelectedNode.IsRoot;

        public ObservableCollection<ConnectorLine> Connectors
        {
            get => _connectors;
            set => SetProperty(ref _connectors, value);
        }

        public double ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                double clamped = Math.Clamp(value, 0.2, 3.0);
                if (SetProperty(ref _zoomLevel, clamped))
                {
                    OnPropertyChanged(nameof(ZoomPercentageText));
                }
            }
        }

        public string ZoomPercentageText => $"{(int)Math.Round(ZoomLevel * 100)}%";

        public double PanX
        {
            get => _panX;
            set => SetProperty(ref _panX, value);
        }

        public double PanY
        {
            get => _panY;
            set => SetProperty(ref _panY, value);
        }

        public string SaveStatusText
        {
            get => _saveStatusText;
            set => SetProperty(ref _saveStatusText, value);
        }

        public bool CanUndo => _undoRedo.CanUndo;
        public bool CanRedo => _undoRedo.CanRedo;

        public bool IsFullscreen
        {
            get => _isFullscreen;
            set => SetProperty(ref _isFullscreen, value);
        }

        public bool IsEditingText
        {
            get => _isEditingText;
            set => SetProperty(ref _isEditingText, value);
        }

        public string PaletteName
        {
            get => _document?.ThemePalette ?? "Vivid Spectrum";
            set
            {
                if (_document != null && _document.ThemePalette != value)
                {
                    RecordUndo("Change Palette");
                    _document.ThemePalette = value;
                    ApplyPaletteToBranches();
                    MarkDirty();
                    OnPropertyChanged();
                }
            }
        }

        public string LayoutType
        {
            get => _document?.LayoutType ?? "MindMap";
            set
            {
                if (_document != null && _document.LayoutType != value)
                {
                    RecordUndo("Change Layout");
                    _document.LayoutType = value;
                    RefreshLayoutAndConnectors(true);
                    MarkDirty();
                    OnPropertyChanged();
                }
            }
        }

        public string LineStyle
        {
            get => _document?.LineStyle ?? "BezierCurved";
            set
            {
                if (_document != null && _document.LineStyle != value)
                {
                    RecordUndo("Change Line Style");
                    _document.LineStyle = value;
                    RefreshConnectors();
                    MarkDirty();
                    OnPropertyChanged();
                }
            }
        }

        // Selected Node Formatting Bindings
        public string SelectedNodeText
        {
            get => SelectedNode?.Text ?? string.Empty;
            set
            {
                if (SelectedNode != null && SelectedNode.Text != value)
                {
                    RecordUndo("Edit Text");
                    SelectedNode.Text = value;
                    LayoutEngine.CalculateNodeDimensions(SelectedNode);
                    RefreshLayoutAndConnectors(false);
                    MarkDirty();
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedNodeNote
        {
            get => SelectedNode?.Note ?? string.Empty;
            set
            {
                if (SelectedNode != null && SelectedNode.Note != value)
                {
                    RecordUndo("Edit Note");
                    SelectedNode.Note = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
                    MarkDirty();
                    OnPropertyChanged();
                }
            }
        }

        public double SelectedNodeFontSize => SelectedNode?.FontSize ?? 14;
        public bool SelectedNodeIsBold => SelectedNode?.FontWeight == "Bold";
        public bool SelectedNodeIsItalic => SelectedNode?.FontStyle == "Italic";
        public bool SelectedNodeIsStrikethrough => SelectedNode?.IsStrikethrough ?? false;
        public string SelectedNodeColor => SelectedNode?.BranchColor ?? "#2563EB";
        public string SelectedNodeShape => SelectedNode?.ShapeType ?? "RoundedRectangle";
        public string? SelectedNodeIcon => SelectedNode?.Icon;

        public event EventHandler? CanvasNeedsRedraw;
        public event EventHandler<MindMapNode>? RequestInlineEdit;

        public EditorViewModel()
        {
            _document = MindMapDocument.CreateDefault();
            _undoRedo.StateChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(CanUndo));
                OnPropertyChanged(nameof(CanRedo));
            };

            // Setup autosave timer (debounced 1.5 seconds)
            var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            if (dispatcherQueue != null)
            {
                _autoSaveTimer = dispatcherQueue.CreateTimer();
                _autoSaveTimer.Interval = TimeSpan.FromMilliseconds(1500);
                _autoSaveTimer.Tick += async (s, e) =>
                {
                    _autoSaveTimer.Stop();
                    if (_isDirty)
                    {
                        await SaveAsync();
                    }
                };
            }
            else
            {
                _autoSaveTimer = null!;
            }
        }

        public void MarkDirty()
        {
            _isDirty = true;
            SaveStatusText = "Unsaved changes";
            _autoSaveTimer?.Stop();
            _autoSaveTimer?.Start();
        }

        public void RecordUndo(string operation)
        {
            if (_document != null)
            {
                _undoRedo.RecordState(_document);
            }
        }

        public void Undo()
        {
            if (_undoRedo.CanUndo && _document != null)
            {
                var restored = _undoRedo.Undo(_document);
                if (restored != null)
                {
                    _document = restored;
                    // Reselect corresponding node or root
                    SelectedNode = _document.RootNode?.FindNode(SelectedNode?.Id ?? "") ?? _document.RootNode;
                    RefreshLayoutAndConnectors(false);
                    MarkDirty();
                    OnPropertyChanged(nameof(Document));
                    OnPropertyChanged(nameof(DocumentTitle));
                }
            }
        }

        public void Redo()
        {
            if (_undoRedo.CanRedo && _document != null)
            {
                var restored = _undoRedo.Redo(_document);
                if (restored != null)
                {
                    _document = restored;
                    SelectedNode = _document.RootNode?.FindNode(SelectedNode?.Id ?? "") ?? _document.RootNode;
                    RefreshLayoutAndConnectors(false);
                    MarkDirty();
                    OnPropertyChanged(nameof(Document));
                    OnPropertyChanged(nameof(DocumentTitle));
                }
            }
        }

        public MindMapNode? AddChildNode(MindMapNode? parent = null)
        {
            parent ??= SelectedNode ?? _document.RootNode;
            if (parent == null) return null;

            RecordUndo("Add Child");

            // Expand parent if it was collapsed
            if (parent.IsCollapsed)
            {
                parent.IsCollapsed = false;
            }

            int branchIdx = parent.IsRoot ? parent.Children.Count : parent.Children.Count;
            string color = parent.IsRoot
                ? ColorPalette.GetColorForBranchIndex(branchIdx, _document.ThemePalette)
                : parent.BranchColor;

            string side = parent.IsRoot ? "Auto" : parent.Side;

            var child = new MindMapNode
            {
                Id = Guid.NewGuid().ToString("N"),
                ParentId = parent.Id,
                Text = "New Topic",
                IsRoot = false,
                BranchColor = color,
                FontSize = parent.IsRoot ? 14 : 13,
                FontWeight = "Normal",
                ShapeType = "RoundedRectangle",
                Side = side,
                Children = new List<MindMapNode>()
            };

            parent.Children.Add(child);
            SelectedNode = child;

            RefreshLayoutAndConnectors(true);
            MarkDirty();

            RequestInlineEdit?.Invoke(this, child);
            return child;
        }

        public MindMapNode? AddSiblingNode(MindMapNode? current = null)
        {
            current ??= SelectedNode;
            if (current == null || current.IsRoot)
            {
                return AddChildNode(_document.RootNode);
            }

            var parent = _document.RootNode.FindParent(current.Id);
            if (parent == null) return null;

            RecordUndo("Add Sibling");

            int currentIndex = parent.Children.IndexOf(current);
            int newBranchIdx = parent.IsRoot ? parent.Children.Count : parent.Children.Count;
            string color = parent.IsRoot
                ? ColorPalette.GetColorForBranchIndex(newBranchIdx, _document.ThemePalette)
                : parent.BranchColor;

            var sibling = new MindMapNode
            {
                Id = Guid.NewGuid().ToString("N"),
                ParentId = parent.Id,
                Text = "New Topic",
                IsRoot = false,
                BranchColor = color,
                FontSize = current.FontSize,
                FontWeight = current.FontWeight,
                ShapeType = current.ShapeType,
                Side = current.Side,
                Children = new List<MindMapNode>()
            };

            if (currentIndex >= 0 && currentIndex < parent.Children.Count)
            {
                parent.Children.Insert(currentIndex + 1, sibling);
            }
            else
            {
                parent.Children.Add(sibling);
            }

            SelectedNode = sibling;
            RefreshLayoutAndConnectors(true);
            MarkDirty();

            RequestInlineEdit?.Invoke(this, sibling);
            return sibling;
        }

        public bool DeleteSelectedNode()
        {
            if (SelectedNode == null || SelectedNode.IsRoot) return false;

            var parent = _document.RootNode.FindParent(SelectedNode.Id);
            if (parent == null) return false;

            RecordUndo("Delete Node");

            int index = parent.Children.IndexOf(SelectedNode);
            parent.Children.Remove(SelectedNode);

            // Select next sibling or parent
            if (parent.Children.Count > 0)
            {
                int newIndex = Math.Clamp(index, 0, parent.Children.Count - 1);
                SelectedNode = parent.Children[newIndex];
            }
            else
            {
                SelectedNode = parent;
            }

            RefreshLayoutAndConnectors(true);
            MarkDirty();
            return true;
        }

        public void ToggleCollapse(MindMapNode? node = null)
        {
            node ??= SelectedNode;
            if (node == null || node.Children.Count == 0) return;

            RecordUndo("Toggle Collapse");
            node.IsCollapsed = !node.IsCollapsed;
            RefreshLayoutAndConnectors(true);
            MarkDirty();
        }

        public void SetNodeColor(string hexColor)
        {
            if (SelectedNode == null) return;

            RecordUndo("Set Color");
            SelectedNode.BranchColor = hexColor;
            // Propagate to descendants if not root
            if (!SelectedNode.IsRoot)
            {
                PropagateColorRecursive(SelectedNode, hexColor);
            }
            RefreshConnectors();
            MarkDirty();
            OnPropertyChanged(nameof(SelectedNodeColor));
            CanvasNeedsRedraw?.Invoke(this, EventArgs.Empty);
        }

        private void PropagateColorRecursive(MindMapNode node, string color)
        {
            foreach (var child in node.Children)
            {
                child.BranchColor = color;
                PropagateColorRecursive(child, color);
            }
        }

        public void SetNodeShape(string shape)
        {
            if (SelectedNode == null) return;
            RecordUndo("Set Shape");
            SelectedNode.ShapeType = shape;
            LayoutEngine.CalculateNodeDimensions(SelectedNode);
            RefreshLayoutAndConnectors(false);
            MarkDirty();
            OnPropertyChanged(nameof(SelectedNodeShape));
        }

        public void SetNodeFontSize(double delta)
        {
            if (SelectedNode == null) return;
            RecordUndo("Set Font Size");
            SelectedNode.FontSize = Math.Clamp(SelectedNode.FontSize + delta, 10, 32);
            LayoutEngine.CalculateNodeDimensions(SelectedNode);
            RefreshLayoutAndConnectors(false);
            MarkDirty();
            OnPropertyChanged(nameof(SelectedNodeFontSize));
        }

        public void ToggleBold()
        {
            if (SelectedNode == null) return;
            RecordUndo("Toggle Bold");
            SelectedNode.FontWeight = (SelectedNode.FontWeight == "Bold") ? "Normal" : "Bold";
            LayoutEngine.CalculateNodeDimensions(SelectedNode);
            RefreshLayoutAndConnectors(false);
            MarkDirty();
            OnPropertyChanged(nameof(SelectedNodeIsBold));
        }

        public void ToggleItalic()
        {
            if (SelectedNode == null) return;
            RecordUndo("Toggle Italic");
            SelectedNode.FontStyle = (SelectedNode.FontStyle == "Italic") ? "Normal" : "Italic";
            LayoutEngine.CalculateNodeDimensions(SelectedNode);
            RefreshLayoutAndConnectors(false);
            MarkDirty();
            OnPropertyChanged(nameof(SelectedNodeIsItalic));
        }

        public void ToggleStrikethrough()
        {
            if (SelectedNode == null) return;
            RecordUndo("Toggle Strikethrough");
            SelectedNode.IsStrikethrough = !SelectedNode.IsStrikethrough;
            MarkDirty();
            OnPropertyChanged(nameof(SelectedNodeIsStrikethrough));
            CanvasNeedsRedraw?.Invoke(this, EventArgs.Empty);
        }

        public void SetNodeIcon(string? icon)
        {
            if (SelectedNode == null) return;
            RecordUndo("Set Icon");
            SelectedNode.Icon = icon;
            LayoutEngine.CalculateNodeDimensions(SelectedNode);
            RefreshLayoutAndConnectors(false);
            MarkDirty();
            OnPropertyChanged(nameof(SelectedNodeIcon));
        }

        public void SetNodeProgress(int? progress)
        {
            if (SelectedNode == null) return;
            RecordUndo("Set Progress");
            SelectedNode.Progress = progress;
            LayoutEngine.CalculateNodeDimensions(SelectedNode);
            RefreshLayoutAndConnectors(false);
            MarkDirty();
            CanvasNeedsRedraw?.Invoke(this, EventArgs.Empty);
        }

        public void MoveNode(MindMapNode node, double newX, double newY)
        {
            node.X = newX;
            node.Y = newY;
            _document.LayoutType = "Freeform";
            RefreshConnectors();
            MarkDirty();
        }

        public void ApplyPaletteToBranches()
        {
            if (_document?.RootNode == null) return;

            var pal = ColorPalette.GetPalette(_document.ThemePalette);
            _document.RootNode.BranchColor = pal.RootColor;
            _document.RootNode.FillColor = pal.RootColor;

            for (int i = 0; i < _document.RootNode.Children.Count; i++)
            {
                var child = _document.RootNode.Children[i];
                string color = ColorPalette.GetColorForBranchIndex(i, _document.ThemePalette);
                child.BranchColor = color;
                PropagateColorRecursive(child, color);
            }

            RefreshConnectors();
            CanvasNeedsRedraw?.Invoke(this, EventArgs.Empty);
        }

        public void RefreshLayoutAndConnectors(bool recalculatePositions = true)
        {
            if (_document?.RootNode == null) return;

            if (recalculatePositions && (_document.LayoutType != "Freeform" || (_document.RootNode.Children.Any(c => c.X == 0 && c.Y == 0))))
            {
                LayoutEngine.ApplyAutoLayout(_document);
            }

            RefreshConnectors();
            CanvasNeedsRedraw?.Invoke(this, EventArgs.Empty);
        }

        public void RefreshConnectors()
        {
            if (_document == null) return;
            var connectors = LayoutEngine.GenerateConnectors(_document);
            Connectors = new ObservableCollection<ConnectorLine>(connectors);
        }

        public void FitToMap(double viewportWidth, double viewportHeight)
        {
            if (_document == null || viewportWidth <= 0 || viewportHeight <= 0) return;

            var bbox = LayoutEngine.GetBoundingBox(_document);
            double mapWidth = Math.Max(bbox.maxX - bbox.minX + 160, 200);
            double mapHeight = Math.Max(bbox.maxY - bbox.minY + 160, 200);

            double scaleX = viewportWidth / mapWidth;
            double scaleY = viewportHeight / mapHeight;
            double targetScale = Math.Clamp(Math.Min(scaleX, scaleY), 0.4, 1.0);

            double centerX = (bbox.minX + bbox.maxX) / 2.0;
            double centerY = (bbox.minY + bbox.maxY) / 2.0;

            ZoomLevel = targetScale;
            PanX = (viewportWidth / 2.0) - (centerX * targetScale);
            PanY = (viewportHeight / 2.0) - (centerY * targetScale);
        }

        public void ResetZoom(double viewportWidth, double viewportHeight)
        {
            ZoomLevel = 1.0;
            double rootX = _document?.RootNode?.X ?? 0.0;
            double rootY = _document?.RootNode?.Y ?? 0.0;
            PanX = (viewportWidth / 2.0) - rootX;
            PanY = (viewportHeight / 2.0) - rootY;
        }

        public async Task<bool> SaveAsync()
        {
            try
            {
                SaveStatusText = "Saving...";
                bool success = await StorageService.Instance.SaveMapAsync(_document);
                if (success)
                {
                    _isDirty = false;
                    SaveStatusText = "Saved";
                }
                else
                {
                    SaveStatusText = "Save failed";
                }
                return success;
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Error in SaveAsync", ex);
                SaveStatusText = "Save failed";
                return false;
            }
        }
    }
}
