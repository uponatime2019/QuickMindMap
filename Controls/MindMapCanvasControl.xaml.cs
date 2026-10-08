using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using QuickMindMap.Helpers;
using QuickMindMap.Models;
using QuickMindMap.Services;
using QuickMindMap.ViewModels;

namespace QuickMindMap.Controls
{
    public sealed partial class MindMapCanvasControl : UserControl
    {
        private EditorViewModel? _viewModel;
        private bool _isPanning;
        private Point _lastPanPoint;
        private bool _isDraggingNode;
        private MindMapNode? _draggedNode;
        private Point _dragStartWorldPoint;
        private Point _nodeOriginalPosition;
        private TextBox? _activeEditorTextBox;
        private MindMapNode? _editingNode;
        private bool _isMinimapVisible = true;
        private bool _isMinimapDragging;

        private readonly Dictionary<string, FrameworkElement> _nodeElementMap = new();

        private bool _initialCenterDone;

        public EditorViewModel? ViewModel
        {
            get => _viewModel;
            set
            {
                if (_viewModel != null)
                {
                    _viewModel.CanvasNeedsRedraw -= OnCanvasNeedsRedraw;
                    _viewModel.RequestInlineEdit -= OnRequestInlineEdit;
                    _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                }

                _viewModel = value;

                if (_viewModel != null)
                {
                    _initialCenterDone = false;
                    _viewModel.CanvasNeedsRedraw += OnCanvasNeedsRedraw;
                    _viewModel.RequestInlineEdit += OnRequestInlineEdit;
                    _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                    FitToMap();
                    SyncTransformWithViewModel();
                    PopulateQuickColors();
                    RedrawCanvas();
                }
            }
        }

        public MindMapCanvasControl()
        {
            this.InitializeComponent();
            this.Loaded += OnLoaded;
            this.SizeChanged += OnSizeChanged;
            this.PointerWheelChanged += OnCanvasPointerWheelChanged;
            this.PointerPressed += OnCanvasPointerPressed;
            this.PointerMoved += OnCanvasPointerMoved;
            this.PointerReleased += OnCanvasPointerReleased;
            this.PointerCaptureLost += OnCanvasPointerCaptureLost;
            this.KeyDown += OnCanvasKeyDown;
            this.IsTabStop = true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            DisplayScaleHelper.GetDesktopScalingRatio(this.XamlRoot);
            PopulateQuickColors();
            if (_viewModel != null)
            {
                _viewModel.RefreshLayoutAndConnectors(true);
                FitToMap();
                RedrawCanvas();
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateMinimap();
            if (!_initialCenterDone && e.NewSize.Width > 100 && e.NewSize.Height > 100 && _viewModel?.Document != null)
            {
                _initialCenterDone = true;
                FitToMap();
            }
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EditorViewModel.ZoomLevel) ||
                e.PropertyName == nameof(EditorViewModel.PanX) ||
                e.PropertyName == nameof(EditorViewModel.PanY))
            {
                SyncTransformWithViewModel();
                UpdateMinimap();
            }
            else if (e.PropertyName == nameof(EditorViewModel.SelectedNode))
            {
                UpdateSelectionVisuals();
            }
            else if (e.PropertyName == nameof(EditorViewModel.Connectors))
            {
                RedrawConnectors();
            }
            else if (e.PropertyName == nameof(EditorViewModel.PaletteName) ||
                     e.PropertyName == nameof(EditorViewModel.LayoutType) ||
                     e.PropertyName == nameof(EditorViewModel.LineStyle))
            {
                PopulateQuickColors();
                RedrawCanvas();
            }
        }

        private void OnCanvasNeedsRedraw(object? sender, EventArgs e) => RedrawCanvas();
        private void OnRequestInlineEdit(object? sender, MindMapNode node) => StartInlineEditing(node);

        private void SyncTransformWithViewModel()
        {
            if (_viewModel == null) return;
            CanvasScale.ScaleX = _viewModel.ZoomLevel;
            CanvasScale.ScaleY = _viewModel.ZoomLevel;
            CanvasTranslate.X = _viewModel.PanX;
            CanvasTranslate.Y = _viewModel.PanY;
            TxtZoomLevel.Text = _viewModel.ZoomPercentageText;
        }

        public (double vw, double vh) GetViewportDimensions()
        {
            double vw = ActualWidth;
            double vh = ActualHeight;
            if (vw <= 100 || vh <= 100)
            {
                if (this.XamlRoot != null && this.XamlRoot.Size.Width > 100)
                {
                    vw = this.XamlRoot.Size.Width;
                    vh = Math.Max(this.XamlRoot.Size.Height - 60, 200);
                }
                else
                {
                    vw = 1200;
                    vh = 800;
                }
            }
            return (vw, vh);
        }

        public void CenterCanvasOnRoot()
        {
            if (_viewModel?.Document == null) return;
            var (vw, vh) = GetViewportDimensions();
            _viewModel.ResetZoom(vw, vh);
            SyncTransformWithViewModel();
            UpdateMinimap();
        }

        public void FitToMap()
        {
            if (_viewModel?.Document == null) return;
            var (vw, vh) = GetViewportDimensions();
            _viewModel.FitToMap(vw, vh);
            SyncTransformWithViewModel();
            UpdateMinimap();
        }

        public void RedrawCanvas()
        {
            if (_viewModel?.Document?.RootNode == null) return;

            NodesLayer.Children.Clear();
            AdornerLayer.Children.Clear();
            _nodeElementMap.Clear();

            var visibleNodes = GetVisibleNodes(_viewModel.Document.RootNode);
            foreach (var node in visibleNodes)
            {
                var el = CreateNodeElement(node);
                _nodeElementMap[node.Id] = el;
                NodesLayer.Children.Add(el);
            }

            RedrawConnectors();
            UpdateSelectionVisuals();
            UpdateMinimap();
        }

        private List<MindMapNode> GetVisibleNodes(MindMapNode root)
        {
            var list = new List<MindMapNode>();
            CollectVisibleNodes(root, list);
            return list;
        }

        private void CollectVisibleNodes(MindMapNode current, List<MindMapNode> list)
        {
            list.Add(current);
            if (current.IsCollapsed) return;

            foreach (var child in current.Children)
            {
                CollectVisibleNodes(child, list);
            }
        }

        private void RedrawConnectors()
        {
            LinesLayer.Children.Clear();
            if (_viewModel?.Connectors == null) return;

            foreach (var conn in _viewModel.Connectors)
            {
                var path = new Microsoft.UI.Xaml.Shapes.Path
                {
                    Stroke = MapColorHelper.HexToBrush(conn.ColorHex, 0.85),
                    StrokeThickness = conn.Thickness,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Data = ParsePathGeometry(conn)
                };
                LinesLayer.Children.Add(path);
            }
        }

        private Geometry ParsePathGeometry(ConnectorLine conn)
        {
            var geom = new PathGeometry();
            var fig = new PathFigure
            {
                StartPoint = new Point(conn.StartX, conn.StartY),
                IsClosed = false
            };

            if (conn.LineStyle == "StraightLine")
            {
                fig.Segments.Add(new LineSegment { Point = new Point(conn.EndX, conn.EndY) });
            }
            else if (conn.LineStyle == "RoundedStep")
            {
                double midX = (conn.StartX + conn.EndX) / 2.0;
                fig.Segments.Add(new LineSegment { Point = new Point(midX, conn.StartY) });
                fig.Segments.Add(new LineSegment { Point = new Point(midX, conn.EndY) });
                fig.Segments.Add(new LineSegment { Point = new Point(conn.EndX, conn.EndY) });
            }
            else
            {
                fig.Segments.Add(new BezierSegment
                {
                    Point1 = new Point(conn.Control1X, conn.Control1Y),
                    Point2 = new Point(conn.Control2X, conn.Control2Y),
                    Point3 = new Point(conn.EndX, conn.EndY)
                });
            }

            geom.Figures.Add(fig);
            return geom;
        }

        private FrameworkElement CreateNodeElement(MindMapNode node)
        {
            var rootContainer = new Grid
            {
                Tag = node,
                Width = node.Width,
                Height = node.Height
            };

            Canvas.SetLeft(rootContainer, node.X - node.Width / 2.0);
            Canvas.SetTop(rootContainer, node.Y - node.Height / 2.0);

            var border = new Border
            {
                Width = node.Width,
                Height = node.Height,
                BorderBrush = MapColorHelper.HexToBrush(node.BranchColor),
                BorderThickness = node.ShapeType == "Underline" ? new Thickness(0, 0, 0, 3) : new Thickness(2)
            };

            if (node.IsRoot)
            {
                border.CornerRadius = new CornerRadius(node.Height / 2.0);
                border.Background = MapColorHelper.HexToBrush(node.BranchColor);
                border.BorderThickness = new Thickness(0);
            }
            else
            {
                switch (node.ShapeType)
                {
                    case "Pill":
                        border.CornerRadius = new CornerRadius(node.Height / 2.0);
                        border.Background = MapColorHelper.GetTintBrush(node.BranchColor, 0.08);
                        break;
                    case "Rectangle":
                        border.CornerRadius = new CornerRadius(2);
                        border.Background = MapColorHelper.GetTintBrush(node.BranchColor, 0.08);
                        break;
                    case "Ellipse":
                        border.CornerRadius = new CornerRadius(node.Height / 2.0);
                        border.Background = MapColorHelper.GetTintBrush(node.BranchColor, 0.08);
                        break;
                    case "Underline":
                        border.CornerRadius = new CornerRadius(0);
                        border.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                        break;
                    default:
                        border.CornerRadius = new CornerRadius(8);
                        border.Background = MapColorHelper.GetTintBrush(node.BranchColor, 0.08);
                        break;
                }
            }

            var stack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 6,
                Padding = new Thickness(8, 2, 8, 2)
            };

            if (!string.IsNullOrEmpty(node.Icon))
            {
                var iconText = new TextBlock
                {
                    Text = node.Icon,
                    FontSize = node.FontSize,
                    VerticalAlignment = VerticalAlignment.Center
                };
                stack.Children.Add(iconText);
            }

            var textBlock = new TextBlock
            {
                Text = node.Text,
                FontSize = node.FontSize,
                FontWeight = node.FontWeight == "Bold" ? Microsoft.UI.Text.FontWeights.Bold :
                             node.FontWeight == "SemiBold" ? Microsoft.UI.Text.FontWeights.SemiBold :
                             Microsoft.UI.Text.FontWeights.Normal,
                FontStyle = node.FontStyle == "Italic" ? Windows.UI.Text.FontStyle.Italic : Windows.UI.Text.FontStyle.Normal,
                Foreground = node.IsRoot ? new SolidColorBrush(Microsoft.UI.Colors.White) : (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"],
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxLines = 1
            };

            if (node.IsStrikethrough)
            {
                textBlock.TextDecorations = Windows.UI.Text.TextDecorations.Strikethrough;
            }

            stack.Children.Add(textBlock);

            if (!string.IsNullOrEmpty(node.Note))
            {
                var noteIcon = new FontIcon
                {
                    Glyph = "",
                    FontSize = 14,
                    Foreground = MapColorHelper.HexToBrush(node.BranchColor),
                    VerticalAlignment = VerticalAlignment.Center
                };
                ToolTipService.SetToolTip(noteIcon, node.Note);
                stack.Children.Add(noteIcon);
            }

            border.Child = stack;
            rootContainer.Children.Add(border);

            if (node.HasChildren)
            {
                var badge = CreateCollapseBadge(node);
                rootContainer.Children.Add(badge);
            }

            rootContainer.PointerPressed += (s, e) => OnNodePointerPressed(node, rootContainer, e);
            rootContainer.DoubleTapped += (s, e) => OnNodeDoubleTapped(node, rootContainer, e);
            rootContainer.RightTapped += (s, e) => OnNodeRightTapped(node, rootContainer, e);

            return rootContainer;
        }

        private FrameworkElement CreateCollapseBadge(MindMapNode node)
        {
            bool isRight = node.Side != "Left";
            var badgeGrid = new Grid
            {
                Width = 18,
                Height = 18,
                HorizontalAlignment = isRight ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = isRight ? new Thickness(0, 0, -9, 0) : new Thickness(-9, 0, 0, 0)
            };

            var badgeBorder = new Border
            {
                Width = 18,
                Height = 18,
                CornerRadius = new CornerRadius(9),
                Background = MapColorHelper.HexToBrush(node.BranchColor),
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.White),
                BorderThickness = new Thickness(1.5),
                Child = new TextBlock
                {
                    Text = node.IsCollapsed ? ("+" + node.Children.Count) : "-",
                    FontSize = 14,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            badgeGrid.Children.Add(badgeBorder);
            badgeGrid.PointerPressed += (s, e) =>
            {
                e.Handled = true;
                _viewModel?.ToggleCollapse(node);
            };

            ToolTipService.SetToolTip(badgeGrid, node.IsCollapsed ? ("Expand (" + node.Children.Count + " children)") : "Collapse branch");
            return badgeGrid;
        }

        private void OnNodePointerPressed(MindMapNode node, FrameworkElement element, PointerRoutedEventArgs e)
        {
            var ptr = e.GetCurrentPoint(this);
            if (ptr.Properties.IsLeftButtonPressed)
            {
                if (_activeEditorTextBox != null)
                {
                    CommitInlineEdit();
                }

                if (_viewModel != null)
                {
                    _viewModel.SelectedNode = node;
                }

                _isDraggingNode = true;
                _draggedNode = node;
                _dragStartWorldPoint = ScreenToWorld(ptr.Position);
                _nodeOriginalPosition = new Point(node.X, node.Y);

                element.CapturePointer(e.Pointer);
                e.Handled = true;
            }
        }

        private void OnNodeDoubleTapped(MindMapNode node, FrameworkElement element, DoubleTappedRoutedEventArgs e)
        {
            e.Handled = true;
            StartInlineEditing(node);
        }

        private void OnNodeRightTapped(MindMapNode node, FrameworkElement element, RightTappedRoutedEventArgs e)
        {
            e.Handled = true;
            if (_viewModel != null)
            {
                _viewModel.SelectedNode = node;
            }
            ShowNodeContextMenu(node, element, e.GetPosition(element));
        }

        private void ShowNodeContextMenu(MindMapNode node, FrameworkElement target, Point position)
        {
            var menu = new MenuFlyout();

            var itemAddChild = new MenuFlyoutItem { Text = "Add Child (Tab)", Icon = new FontIcon { Glyph = "" } };
            itemAddChild.Click += (s, e) => _viewModel?.AddChildNode(node);
            menu.Items.Add(itemAddChild);

            if (!node.IsRoot)
            {
                var itemAddSibling = new MenuFlyoutItem { Text = "Add Sibling (Enter)", Icon = new FontIcon { Glyph = "" } };
                itemAddSibling.Click += (s, e) => _viewModel?.AddSiblingNode(node);
                menu.Items.Add(itemAddSibling);
            }

            var itemEdit = new MenuFlyoutItem { Text = "Edit Text (F2)", Icon = new FontIcon { Glyph = "" } };
            itemEdit.Click += (s, e) => StartInlineEditing(node);
            menu.Items.Add(itemEdit);

            menu.Items.Add(new MenuFlyoutSeparator());

            if (node.HasChildren)
            {
                var itemCollapse = new MenuFlyoutItem
                {
                    Text = node.IsCollapsed ? "Expand Branch (Space)" : "Collapse Branch (Space)",
                    Icon = new FontIcon { Glyph = node.IsCollapsed ? "" : "" }
                };
                itemCollapse.Click += (s, e) => _viewModel?.ToggleCollapse(node);
                menu.Items.Add(itemCollapse);
            }

            if (!node.IsRoot)
            {
                var itemDelete = new MenuFlyoutItem { Text = "Delete Node (Del)", Icon = new FontIcon { Glyph = "" } };
                itemDelete.Click += (s, e) => _viewModel?.DeleteSelectedNode();
                menu.Items.Add(itemDelete);
            }

            menu.ShowAt(target, position);
        }

        public void StartInlineEditing(MindMapNode? node = null)
        {
            node ??= _viewModel?.SelectedNode;
            if (node == null) return;

            if (!_nodeElementMap.TryGetValue(node.Id, out var nodeElement) || !(nodeElement is Grid container))
            {
                return;
            }

            if (container.Children[0] is Border border && border.Child is StackPanel stack)
            {
                _editingNode = node;

                double scaleRatio = DisplayScaleHelper.GetDesktopScalingRatio(this.XamlRoot);
                if (scaleRatio < 1.0) scaleRatio = 1.0;

                // Make the editing box bigger calculated by multiplying with current desktop scaling ratio
                double estimatedCharWidth = node.FontSize * 0.70;
                string currentText = string.IsNullOrWhiteSpace(node.Text) ? "New Topic" : node.Text;
                double estimatedTextWidth = currentText.Length * estimatedCharWidth;

                // Generous clearance for WinUI TextBox internal clear 'x' button (~34px) + padding + borders
                double extraClearAndPadding = 56.0 * scaleRatio;
                double editWidth = Math.Max(node.Width + 24.0 * scaleRatio, (estimatedTextWidth + extraClearAndPadding) * scaleRatio);
                editWidth = Math.Max(editWidth, 140.0 * scaleRatio);
                double editHeight = Math.Max(node.Height + 4.0 * scaleRatio, 40.0 * scaleRatio);

                container.Width = editWidth;
                container.Height = editHeight;
                border.Width = editWidth;
                border.Height = editHeight;
                Canvas.SetLeft(container, node.X - editWidth / 2.0);
                Canvas.SetTop(container, node.Y - editHeight / 2.0);

                var textBox = new TextBox
                {
                    Text = node.Text,
                    FontSize = node.FontSize,
                    FontWeight = node.FontWeight == "Bold" ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Normal,
                    Width = editWidth - 4,
                    Height = editHeight - 4,
                    MinWidth = 120 * scaleRatio,
                    MaxWidth = 450 * scaleRatio,
                    Padding = new Thickness(8 * scaleRatio, 2 * scaleRatio, 8 * scaleRatio, 2 * scaleRatio),
                    BorderThickness = new Thickness(1),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                textBox.TextChanged += (s, e) =>
                {
                    string txt = textBox.Text ?? string.Empty;
                    double neededWidth = Math.Max(140.0 * scaleRatio, (txt.Length * estimatedCharWidth + extraClearAndPadding) * scaleRatio);
                    if (neededWidth > container.Width)
                    {
                        container.Width = neededWidth;
                        border.Width = neededWidth;
                        textBox.Width = neededWidth - 4;
                        Canvas.SetLeft(container, node.X - neededWidth / 2.0);
                        UpdateSelectionVisuals();
                    }
                };

                textBox.KeyDown += (s, e) =>
                {
                    if (e.Key == VirtualKey.Enter)
                    {
                        e.Handled = true;
                        CommitInlineEdit();
                    }
                    else if (e.Key == VirtualKey.Escape)
                    {
                        e.Handled = true;
                        CancelInlineEdit();
                    }
                };

                textBox.LostFocus += (s, e) => CommitInlineEdit();

                stack.Visibility = Visibility.Collapsed;
                container.Children.Add(textBox);
                _activeEditorTextBox = textBox;

                textBox.Focus(FocusState.Programmatic);
                textBox.SelectAll();
                UpdateSelectionVisuals();
            }
        }

        public void CommitInlineEdit()
        {
            if (_activeEditorTextBox != null && _editingNode != null && _viewModel != null)
            {
                string newText = _activeEditorTextBox.Text?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(newText))
                {
                    _viewModel.SelectedNodeText = newText;
                }

                _activeEditorTextBox = null;
                _editingNode = null;
                RedrawCanvas();
            }
        }

        public void CancelInlineEdit()
        {
            _activeEditorTextBox = null;
            _editingNode = null;
            RedrawCanvas();
        }

        private void UpdateSelectionVisuals()
        {
            AdornerLayer.Children.Clear();
            var selected = _viewModel?.SelectedNode;
            if (selected == null)
            {
                MiniToolbar.Visibility = Visibility.Collapsed;
                return;
            }

            double width = selected.Width;
            double height = selected.Height;

            // If actively editing this node, match the expanded editor container size
            if (_activeEditorTextBox != null && _editingNode == selected &&
                _nodeElementMap.TryGetValue(selected.Id, out var elem) && elem is Grid g)
            {
                width = g.Width;
                height = g.Height;
            }

            double padding = 4;
            var selBox = new Border
            {
                Width = width + padding * 2,
                Height = height + padding * 2,
                BorderBrush = MapColorHelper.GetSystemAccentBrush(),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(height / 2.0 + 2),
                IsHitTestVisible = false
            };

            Canvas.SetLeft(selBox, selected.X - width / 2.0 - padding);
            Canvas.SetTop(selBox, selected.Y - height / 2.0 - padding);
            AdornerLayer.Children.Add(selBox);

            var worldPos = new Point(selected.X, selected.Y - selected.Height / 2.0 - 12);
            var screenPos = WorldToScreen(worldPos);

            MiniToolbar.Visibility = Visibility.Visible;
            MiniToolbar.Margin = new Thickness(
                Math.Max(10, screenPos.X - 110),
                Math.Max(10, screenPos.Y - 45),
                0, 0);

            BtnAddSibling.IsEnabled = !selected.IsRoot;
            BtnMiniDelete.IsEnabled = !selected.IsRoot;
        }

        private void PopulateQuickColors()
        {
            QuickColorGrid.Children.Clear();
            var pal = ColorPalette.GetPalette(_viewModel?.PaletteName);

            var colors = new List<string> { pal.RootColor };
            colors.AddRange(pal.BranchColors);

            foreach (var hex in colors.Distinct())
            {
                var btn = new Button
                {
                    Width = 28,
                    Height = 28,
                    Padding = new Thickness(0),
                    Margin = new Thickness(2),
                    CornerRadius = new CornerRadius(14),
                    Background = MapColorHelper.HexToBrush(hex),
                    BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.White),
                    BorderThickness = new Thickness(1.5),
                    Tag = hex
                };

                btn.Click += (s, e) =>
                {
                    if (s is Button b && b.Tag is string colorHex)
                    {
                        _viewModel?.SetNodeColor(colorHex);
                        BtnQuickColor.Flyout?.Hide();
                    }
                };

                QuickColorGrid.Children.Add(btn);
            }
        }

        private void OnCanvasPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var ptr = e.GetCurrentPoint(this);
            if (ptr.Properties.IsMiddleButtonPressed || ptr.Properties.IsLeftButtonPressed)
            {
                if (_activeEditorTextBox != null)
                {
                    CommitInlineEdit();
                }

                _isPanning = true;
                _lastPanPoint = ptr.Position;
                CapturePointer(e.Pointer);
                e.Handled = true;
            }
        }

        private void OnCanvasPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var ptr = e.GetCurrentPoint(this);

            if (_isDraggingNode && _draggedNode != null)
            {
                var curWorld = ScreenToWorld(ptr.Position);
                double dx = curWorld.X - _dragStartWorldPoint.X;
                double dy = curWorld.Y - _dragStartWorldPoint.Y;

                _draggedNode.X = _nodeOriginalPosition.X + dx;
                _draggedNode.Y = _nodeOriginalPosition.Y + dy;

                if (_nodeElementMap.TryGetValue(_draggedNode.Id, out var el))
                {
                    Canvas.SetLeft(el, _draggedNode.X - _draggedNode.Width / 2.0);
                    Canvas.SetTop(el, _draggedNode.Y - _draggedNode.Height / 2.0);
                }

                _viewModel?.RefreshConnectors();
                UpdateSelectionVisuals();
                UpdateMinimap();
                e.Handled = true;
                return;
            }

            if (_isPanning)
            {
                double dx = ptr.Position.X - _lastPanPoint.X;
                double dy = ptr.Position.Y - _lastPanPoint.Y;
                _lastPanPoint = ptr.Position;

                CanvasTranslate.X += dx;
                CanvasTranslate.Y += dy;

                if (_viewModel != null)
                {
                    _viewModel.PanX = CanvasTranslate.X;
                    _viewModel.PanY = CanvasTranslate.Y;
                }

                UpdateSelectionVisuals();
                UpdateMinimap();
                e.Handled = true;
            }
        }

        private void OnCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isDraggingNode && _draggedNode != null)
            {
                _isDraggingNode = false;
                _draggedNode = null;
                _viewModel?.RecordUndo("Move Node");
                _viewModel?.MarkDirty();
                ReleasePointerCapture(e.Pointer);
                e.Handled = true;
                return;
            }

            if (_isPanning)
            {
                _isPanning = false;
                ReleasePointerCapture(e.Pointer);
                e.Handled = true;
            }
        }

        private void OnCanvasPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            _isPanning = false;
            _isDraggingNode = false;
            _draggedNode = null;
        }

        private void OnCanvasPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var ptr = e.GetCurrentPoint(this);
            int delta = ptr.Properties.MouseWheelDelta;
            if (delta == 0 || _viewModel == null) return;

            double zoomFactor = delta > 0 ? 1.15 : 0.87;
            double oldZoom = _viewModel.ZoomLevel;
            double newZoom = Math.Clamp(oldZoom * zoomFactor, 0.2, 3.0);

            if (Math.Abs(newZoom - oldZoom) > 0.001)
            {
                Point mousePos = ptr.Position;
                double mouseWorldX = (mousePos.X - CanvasTranslate.X) / oldZoom;
                double mouseWorldY = (mousePos.Y - CanvasTranslate.Y) / oldZoom;

                _viewModel.ZoomLevel = newZoom;
                _viewModel.PanX = mousePos.X - mouseWorldX * newZoom;
                _viewModel.PanY = mousePos.Y - mouseWorldY * newZoom;

                SyncTransformWithViewModel();
                UpdateSelectionVisuals();
                UpdateMinimap();
            }

            e.Handled = true;
        }

        private void OnCanvasKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (_activeEditorTextBox != null) return;
            if (_viewModel == null) return;

            bool isCtrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            switch (e.Key)
            {
                case VirtualKey.Tab:
                    e.Handled = true;
                    _viewModel.AddChildNode();
                    break;
                case VirtualKey.Enter:
                    e.Handled = true;
                    _viewModel.AddSiblingNode();
                    break;
                case VirtualKey.F2:
                    e.Handled = true;
                    StartInlineEditing();
                    break;
                case VirtualKey.Delete:
                case VirtualKey.Back:
                    e.Handled = true;
                    _viewModel.DeleteSelectedNode();
                    break;
                case VirtualKey.Space:
                    e.Handled = true;
                    _viewModel.ToggleCollapse();
                    break;
                case VirtualKey.Z when isCtrl:
                    e.Handled = true;
                    _viewModel.Undo();
                    break;
                case VirtualKey.Y when isCtrl:
                    e.Handled = true;
                    _viewModel.Redo();
                    break;
                case VirtualKey.Number0 when isCtrl:
                case VirtualKey.NumberPad0 when isCtrl:
                    e.Handled = true;
                    CenterCanvasOnRoot();
                    break;
                case VirtualKey.F when isCtrl:
                    e.Handled = true;
                    FitToMap();
                    break;
                case VirtualKey.Right:
                    NavigateSelection("Right");
                    e.Handled = true;
                    break;
                case VirtualKey.Left:
                    NavigateSelection("Left");
                    e.Handled = true;
                    break;
                case VirtualKey.Up:
                    NavigateSelection("Up");
                    e.Handled = true;
                    break;
                case VirtualKey.Down:
                    NavigateSelection("Down");
                    e.Handled = true;
                    break;
            }
        }

        private void NavigateSelection(string direction)
        {
            var cur = _viewModel?.SelectedNode;
            var doc = _viewModel?.Document;
            if (cur == null || doc?.RootNode == null) return;

            if (cur.IsRoot)
            {
                if (direction == "Right")
                {
                    var right = cur.Children.FirstOrDefault(c => c.Side != "Left");
                    if (right != null) _viewModel.SelectedNode = right;
                }
                else if (direction == "Left")
                {
                    var left = cur.Children.FirstOrDefault(c => c.Side == "Left");
                    if (left != null) _viewModel.SelectedNode = left;
                }
                else if (cur.Children.Count > 0)
                {
                    _viewModel.SelectedNode = cur.Children[0];
                }
            }
            else
            {
                var parent = doc.RootNode.FindParent(cur.Id);
                if (direction == "Right")
                {
                    if (cur.Side == "Left")
                    {
                        if (parent != null) _viewModel.SelectedNode = parent;
                    }
                    else
                    {
                        if (!cur.IsCollapsed && cur.Children.Count > 0)
                        {
                            _viewModel.SelectedNode = cur.Children[0];
                        }
                    }
                }
                else if (direction == "Left")
                {
                    if (cur.Side == "Right")
                    {
                        if (parent != null) _viewModel.SelectedNode = parent;
                    }
                    else
                    {
                        if (!cur.IsCollapsed && cur.Children.Count > 0)
                        {
                            _viewModel.SelectedNode = cur.Children[0];
                        }
                    }
                }
                else if (parent != null)
                {
                    int idx = parent.Children.IndexOf(cur);
                    if (direction == "Up" && idx > 0)
                    {
                        _viewModel.SelectedNode = parent.Children[idx - 1];
                    }
                    else if (direction == "Down" && idx < parent.Children.Count - 1)
                    {
                        _viewModel.SelectedNode = parent.Children[idx + 1];
                    }
                }
            }
        }

        private void OnMiniAddChildClick(object sender, RoutedEventArgs e) => _viewModel?.AddChildNode();
        private void OnMiniAddSiblingClick(object sender, RoutedEventArgs e) => _viewModel?.AddSiblingNode();
        private void OnMiniEditClick(object sender, RoutedEventArgs e) => StartInlineEditing();
        private void OnMiniDeleteClick(object sender, RoutedEventArgs e) => _viewModel?.DeleteSelectedNode();

        private void OnShapeMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is string shape)
            {
                _viewModel?.SetNodeShape(shape);
            }
        }

        private void OnZoomInClick(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.ZoomLevel += 0.15;
                SyncTransformWithViewModel();
                UpdateMinimap();
            }
        }

        private void OnZoomOutClick(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.ZoomLevel -= 0.15;
                SyncTransformWithViewModel();
                UpdateMinimap();
            }
        }

        private void OnResetZoomClick(object sender, RoutedEventArgs e) => CenterCanvasOnRoot();
        private void OnFitToMapClick(object sender, RoutedEventArgs e) => FitToMap();

        private void OnToggleMinimapClick(object sender, RoutedEventArgs e)
        {
            _isMinimapVisible = !_isMinimapVisible;
            MinimapBorder.Visibility = _isMinimapVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateMinimap()
        {
            if (!_isMinimapVisible || _viewModel?.Document?.RootNode == null) return;

            for (int i = MinimapCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (MinimapCanvas.Children[i] != MinimapViewportBox)
                {
                    MinimapCanvas.Children.RemoveAt(i);
                }
            }

            var bbox = LayoutEngine.GetBoundingBox(_viewModel.Document);
            double mapW = Math.Max(bbox.maxX - bbox.minX + 100, 300);
            double mapH = Math.Max(bbox.maxY - bbox.minY + 100, 200);

            double miniW = MinimapBorder.ActualWidth > 0 ? MinimapBorder.ActualWidth - 4 : 176;
            double miniH = MinimapBorder.ActualHeight > 0 ? MinimapBorder.ActualHeight - 28 : 100;

            double scale = Math.Min(miniW / mapW, miniH / mapH);
            double mapCenterX = (bbox.minX + bbox.maxX) / 2.0;
            double mapCenterY = (bbox.minY + bbox.maxY) / 2.0;

            var visibleNodes = GetVisibleNodes(_viewModel.Document.RootNode);
            foreach (var node in visibleNodes)
            {
                double miniX = (miniW / 2.0) + (node.X - mapCenterX) * scale;
                double miniY = (miniH / 2.0) + (node.Y - mapCenterY) * scale;

                var dot = new Border
                {
                    Width = Math.Max(node.Width * scale, 6),
                    Height = Math.Max(node.Height * scale, 3),
                    Background = MapColorHelper.HexToBrush(node.BranchColor),
                    CornerRadius = new CornerRadius(1.5),
                    IsHitTestVisible = false
                };

                Canvas.SetLeft(dot, miniX - dot.Width / 2.0);
                Canvas.SetTop(dot, miniY - dot.Height / 2.0);
                MinimapCanvas.Children.Add(dot);
            }

            double vw = ActualWidth > 0 ? ActualWidth : 800;
            double vh = ActualHeight > 0 ? ActualHeight : 600;
            double currentZoom = _viewModel.ZoomLevel;

            double viewWorldW = vw / currentZoom;
            double viewWorldH = vh / currentZoom;

            double viewWorldX = (-CanvasTranslate.X) / currentZoom;
            double viewWorldY = (-CanvasTranslate.Y) / currentZoom;

            double vpBoxW = Math.Clamp(viewWorldW * scale, 12, miniW);
            double vpBoxH = Math.Clamp(viewWorldH * scale, 8, miniH);

            double vpBoxX = (miniW / 2.0) + (viewWorldX - mapCenterX) * scale;
            double vpBoxY = (miniH / 2.0) + (viewWorldY - mapCenterY) * scale;

            MinimapViewportBox.Width = vpBoxW;
            MinimapViewportBox.Height = vpBoxH;
            Canvas.SetLeft(MinimapViewportBox, vpBoxX);
            Canvas.SetTop(MinimapViewportBox, vpBoxY);
        }

        private void OnMinimapPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isMinimapDragging = true;
            PanViaMinimap(e.GetCurrentPoint(MinimapCanvas).Position);
            e.Handled = true;
        }

        private void OnMinimapPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isMinimapDragging)
            {
                PanViaMinimap(e.GetCurrentPoint(MinimapCanvas).Position);
                e.Handled = true;
            }
        }

        private void PanViaMinimap(Point miniPos)
        {
            if (_viewModel?.Document?.RootNode == null) return;

            var bbox = LayoutEngine.GetBoundingBox(_viewModel.Document);
            double mapW = Math.Max(bbox.maxX - bbox.minX + 100, 300);
            double mapH = Math.Max(bbox.maxY - bbox.minY + 100, 200);

            double miniW = MinimapBorder.ActualWidth > 0 ? MinimapBorder.ActualWidth - 4 : 176;
            double miniH = MinimapBorder.ActualHeight > 0 ? MinimapBorder.ActualHeight - 28 : 100;

            double scale = Math.Min(miniW / mapW, miniH / mapH);
            double mapCenterX = (bbox.minX + bbox.maxX) / 2.0;
            double mapCenterY = (bbox.minY + bbox.maxY) / 2.0;

            double targetWorldX = mapCenterX + (miniPos.X - miniW / 2.0) / scale;
            double targetWorldY = mapCenterY + (miniPos.Y - miniH / 2.0) / scale;

            double vw = ActualWidth > 0 ? ActualWidth : 800;
            double vh = ActualHeight > 0 ? ActualHeight : 600;

            _viewModel.PanX = (vw / 2.0) - (targetWorldX * _viewModel.ZoomLevel);
            _viewModel.PanY = (vh / 2.0) - (targetWorldY * _viewModel.ZoomLevel);

            SyncTransformWithViewModel();
            UpdateMinimap();
        }

        public Point ScreenToWorld(Point screen)
        {
            double z = _viewModel?.ZoomLevel ?? 1.0;
            return new Point(
                (screen.X - CanvasTranslate.X) / z,
                (screen.Y - CanvasTranslate.Y) / z);
        }

        public Point WorldToScreen(Point world)
        {
            double z = _viewModel?.ZoomLevel ?? 1.0;
            return new Point(
                world.X * z + CanvasTranslate.X,
                world.Y * z + CanvasTranslate.Y);
        }
    }
}
