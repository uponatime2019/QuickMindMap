using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using QuickMindMap.Helpers;
using QuickMindMap.Models;
using QuickMindMap.Services;
using QuickMindMap.ViewModels;

namespace QuickMindMap.Views
{
    public sealed partial class EditorView : UserControl
    {
        private readonly EditorViewModel _viewModel = new();
        public event EventHandler? RequestBackToLibrary;

        public EditorViewModel ViewModel => _viewModel;

        public EditorView()
        {
            this.InitializeComponent();
            MapCanvas.ViewModel = _viewModel;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            DisplayScaleHelper.GetDesktopScalingRatio(this.XamlRoot);
            PopulatePaletteCombo();
            PopulateToolbarColors();
            UpdateUIFromViewModel();
        }

        public void LoadDocument(MindMapDocument document)
        {
            DisplayScaleHelper.GetDesktopScalingRatio(this.XamlRoot);
            _viewModel.Document = document;
            MapCanvas.ViewModel = _viewModel;
            UpdateUIFromViewModel();
            PopulateToolbarColors();

            MapCanvas.FitToMap();

            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
            {
                MapCanvas.FitToMap();
            });
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EditorViewModel.DocumentTitle))
            {
                if (TxtMapTitle.Text != _viewModel.DocumentTitle)
                {
                    TxtMapTitle.Text = _viewModel.DocumentTitle;
                }
            }
            else if (e.PropertyName == nameof(EditorViewModel.SaveStatusText))
            {
                TxtSaveStatus.Text = _viewModel.SaveStatusText;
            }
            else if (e.PropertyName == nameof(EditorViewModel.CanUndo))
            {
                BtnUndo.IsEnabled = _viewModel.CanUndo;
            }
            else if (e.PropertyName == nameof(EditorViewModel.CanRedo))
            {
                BtnRedo.IsEnabled = _viewModel.CanRedo;
            }
            else if (e.PropertyName == nameof(EditorViewModel.SelectedNode))
            {
                UpdateNodeFormattingControls();
            }
            else if (e.PropertyName == nameof(EditorViewModel.PaletteName))
            {
                PopulateToolbarColors();
            }
        }

        private void UpdateUIFromViewModel()
        {
            TxtMapTitle.Text = _viewModel.DocumentTitle;
            TxtSaveStatus.Text = _viewModel.SaveStatusText;
            BtnUndo.IsEnabled = _viewModel.CanUndo;
            BtnRedo.IsEnabled = _viewModel.CanRedo;
            UpdateNodeFormattingControls();
        }

        private void UpdateNodeFormattingControls()
        {
            var node = _viewModel.SelectedNode;
            bool hasNode = node != null;

            BtnAddChild.IsEnabled = hasNode;
            BtnAddSibling.IsEnabled = hasNode && !node.IsRoot;
            BtnDeleteNode.IsEnabled = hasNode && !node.IsRoot;

            ToggleBold.IsChecked = _viewModel.SelectedNodeIsBold;
            ToggleItalic.IsChecked = _viewModel.SelectedNodeIsItalic;
            ToggleStrikethrough.IsChecked = _viewModel.SelectedNodeIsStrikethrough;

            if (node != null)
            {
                TxtNodeNote.Text = node.Note ?? string.Empty;
            }
        }

        private void PopulatePaletteCombo()
        {
            ComboPalette.Items.Clear();
            foreach (var pal in ColorPalette.AvailablePalettes)
            {
                var item = new ComboBoxItem { Content = pal.Name, Tag = pal.Name };
                ComboPalette.Items.Add(item);
                if (pal.Name == _viewModel.PaletteName)
                {
                    ComboPalette.SelectedItem = item;
                }
            }
            if (ComboPalette.SelectedIndex == -1) ComboPalette.SelectedIndex = 0;
        }

        private void PopulateToolbarColors()
        {
            ToolbarColorGrid.Children.Clear();
            var pal = ColorPalette.GetPalette(_viewModel.PaletteName);

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
                    BorderBrush = new SolidColorBrush(Colors.White),
                    BorderThickness = new Thickness(1.5),
                    Tag = hex
                };

                btn.Click += (s, e) =>
                {
                    if (s is Button b && b.Tag is string colorHex)
                    {
                        _viewModel.SetNodeColor(colorHex);
                    }
                };

                ToolbarColorGrid.Children.Add(btn);
            }
        }

        private async void OnBackClick(object sender, RoutedEventArgs e)
        {
            await _viewModel.SaveAsync();
            RequestBackToLibrary?.Invoke(this, EventArgs.Empty);
        }

        private void OnMapTitleChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TxtMapTitle.Text) && TxtMapTitle.Text != _viewModel.DocumentTitle)
            {
                _viewModel.DocumentTitle = TxtMapTitle.Text.Trim();
            }
        }

        private void OnUndoClick(object sender, RoutedEventArgs e) => _viewModel.Undo();
        private void OnRedoClick(object sender, RoutedEventArgs e) => _viewModel.Redo();

        private void OnAddChildClick(object sender, RoutedEventArgs e) => _viewModel.AddChildNode();
        private void OnAddSiblingClick(object sender, RoutedEventArgs e) => _viewModel.AddSiblingNode();
        private void OnDeleteClick(object sender, RoutedEventArgs e) => _viewModel.DeleteSelectedNode();

        private void OnDecreaseFontClick(object sender, RoutedEventArgs e) => _viewModel.SetNodeFontSize(-1);
        private void OnIncreaseFontClick(object sender, RoutedEventArgs e) => _viewModel.SetNodeFontSize(1);

        private void OnToggleBoldClick(object sender, RoutedEventArgs e) => _viewModel.ToggleBold();
        private void OnToggleItalicClick(object sender, RoutedEventArgs e) => _viewModel.ToggleItalic();
        private void OnToggleStrikethroughClick(object sender, RoutedEventArgs e) => _viewModel.ToggleStrikethrough();

        private void OnShapeSelectClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is string shape)
            {
                _viewModel.SetNodeShape(shape);
            }
        }

        private void OnPaletteSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboPalette.SelectedItem is ComboBoxItem item && item.Tag is string palName)
            {
                _viewModel.PaletteName = palName;
            }
        }

        private void OnIconSelectClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string icon)
            {
                _viewModel.SetNodeIcon(string.IsNullOrEmpty(icon) ? null : icon);
            }
        }

        private void OnNodeNoteChanged(object sender, TextChangedEventArgs e)
        {
            if (_viewModel.SelectedNode != null)
            {
                _viewModel.SelectedNodeNote = TxtNodeNote.Text;
            }
        }

        private void OnLayoutSelectClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is string layout)
            {
                _viewModel.LayoutType = layout;
            }
        }

        private void OnLineStyleSelectClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is string lineStyle)
            {
                _viewModel.LineStyle = lineStyle;
            }
        }

        private async void OnSaveClick(object sender, RoutedEventArgs e)
        {
            await _viewModel.SaveAsync();
        }

        private async void OnExportPngClick(object sender, RoutedEventArgs e)
        {
            var win = (Application.Current as App)?.MainWindow;
            if (win != null)
            {
                await ExportService.Instance.ExportAsPngAsync(MapCanvas, win, _viewModel.DocumentTitle);
            }
        }

        private async void OnExportJsonClick(object sender, RoutedEventArgs e)
        {
            var win = (Application.Current as App)?.MainWindow;
            if (win != null)
            {
                await ExportService.Instance.ExportAsJsonAsync(_viewModel.Document, win);
            }
        }

        private async void OnExportMarkdownClick(object sender, RoutedEventArgs e)
        {
            var win = (Application.Current as App)?.MainWindow;
            if (win != null)
            {
                await ExportService.Instance.ExportAsMarkdownAsync(_viewModel.Document, win);
            }
        }

        private async void OnExportOpmlClick(object sender, RoutedEventArgs e)
        {
            var win = (Application.Current as App)?.MainWindow;
            if (win != null)
            {
                await ExportService.Instance.ExportAsOpmlAsync(_viewModel.Document, win);
            }
        }

        private async void OnExportPlainTextClick(object sender, RoutedEventArgs e)
        {
            var win = (Application.Current as App)?.MainWindow;
            if (win != null)
            {
                await ExportService.Instance.ExportAsPlainTextAsync(_viewModel.Document, win);
            }
        }

        private void OnFullscreenClick(object sender, RoutedEventArgs e)
        {
            var win = (Application.Current as App)?.MainWindow;
            if (win?.AppWindow != null)
            {
                if (win.AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen)
                {
                    win.AppWindow.SetPresenter(AppWindowPresenterKind.Default);
                }
                else
                {
                    win.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                }
            }
        }

        private async void OnHelpClick(object sender, RoutedEventArgs e)
        {
            var scroll = new ScrollViewer { MaxHeight = 440 };
            var stack = new StackPanel { Spacing = 16, Padding = new Thickness(4) };

            stack.Children.Add(new TextBlock { Text = "Quick MindMap — Quick Guide & Keyboard Shortcuts", FontWeight = Microsoft.UI.Text.FontWeights.Bold, FontSize = 15 });

            var gridShortcuts = new Grid { ColumnSpacing = 16, RowSpacing = 8 };
            gridShortcuts.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            gridShortcuts.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            string[,] shortcuts = new string[,]
            {
                { "Tab / Insert", "Add Child topic to selected node" },
                { "Enter", "Add Sibling topic at the same level" },
                { "F2 / Double Click", "Edit text inline" },
                { "Delete / Backspace", "Delete selected topic branch" },
                { "Spacebar", "Collapse / Expand child branch" },
                { "Arrow Keys", "Navigate between parent, child, and siblings" },
                { "Ctrl + Z", "Undo last change" },
                { "Ctrl + Y", "Redo change" },
                { "Ctrl + 0", "Reset zoom to 100%" },
                { "Ctrl + F", "Fit entire mind map into view" },
                { "Mouse Wheel", "Zoom in / out" },
                { "Drag Canvas", "Pan across infinite mind map" },
                { "Drag Node", "Reposition node in freeform space" },
                { "F11", "Toggle Fullscreen mode" }
            };

            for (int i = 0; i < shortcuts.GetLength(0); i++)
            {
                gridShortcuts.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var borderKey = new Border
                {
                    Background = (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"],
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 2, 6, 2),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                borderKey.Child = new TextBlock { Text = shortcuts[i, 0], FontFamily = new FontFamily("Consolas"), FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
                Grid.SetRow(borderKey, i);
                Grid.SetColumn(borderKey, 0);
                gridShortcuts.Children.Add(borderKey);

                var txtDesc = new TextBlock { Text = shortcuts[i, 1], FontSize = 14, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetRow(txtDesc, i);
                Grid.SetColumn(txtDesc, 1);
                gridShortcuts.Children.Add(txtDesc);
            }

            stack.Children.Add(gridShortcuts);
            scroll.Content = stack;

            var dialog = new ContentDialog
            {
                Title = "Help & Shortcuts",
                Content = scroll,
                CloseButtonText = "Got it",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
