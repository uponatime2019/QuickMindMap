using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using QuickMindMap.Helpers;
using QuickMindMap.Models;
using QuickMindMap.Services;
using QuickMindMap.ViewModels;

namespace QuickMindMap.Views
{
    public sealed partial class LibraryView : UserControl
    {
        private readonly LibraryViewModel _viewModel = new();
        public event EventHandler<MindMapDocument>? RequestOpenMap;

        public LibraryViewModel ViewModel => _viewModel;

        public LibraryView()
        {
            this.InitializeComponent();
            this.Loaded += OnLoaded;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadMapsAsync();
            UpdateView();
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LibraryViewModel.GroupedMaps) ||
                e.PropertyName == nameof(LibraryViewModel.IsEmpty))
            {
                UpdateView();
            }
        }

        private void UpdateView()
        {
            GroupsItemsControl.ItemsSource = _viewModel.GroupedMaps;
            EmptyStateContainer.Visibility = _viewModel.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
            GroupsItemsControl.Visibility = _viewModel.IsEmpty ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void OnMapTileClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is MindMapMetadata meta)
            {
                var doc = !string.IsNullOrEmpty(meta.FilePath) && System.IO.File.Exists(meta.FilePath)
                    ? await StorageService.Instance.LoadMapAsync(meta.FilePath)
                    : await StorageService.Instance.LoadMapAsync(meta.Id);

                if (doc != null)
                {
                    RequestOpenMap?.Invoke(this, doc);
                }
            }
        }

        private void OnFilterModeClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string mode)
            {
                _viewModel.FilterMode = mode;

                // Update button visual styles
                BtnSortName.FontWeight = mode == "Name" ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
                BtnSortGroup.FontWeight = mode == "Group" ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
                BtnSortRecent.FontWeight = mode == "Recent" ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
            }
        }

        private void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            _viewModel.SearchText = sender.Text;
        }

        private async void OnNewMapClick(object sender, RoutedEventArgs e)
        {
            await ShowNewMapDialogAsync();
        }

        private async void OnQuickTemplateClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string templateId)
            {
                var template = MindMapTemplate.Templates.FirstOrDefault(t => t.Id == templateId) ?? MindMapTemplate.Templates[0];
                var doc = await _viewModel.CreateMapFromTemplateAsync(template);
                RequestOpenMap?.Invoke(this, doc);
            }
        }

        private async void OnImportClick(object sender, RoutedEventArgs e)
        {
            var window = (Application.Current as App)?.MainWindow;
            if (window == null) return;

            var imported = await ExportService.Instance.ImportMapFileAsync(window);
            if (imported != null)
            {
                await _viewModel.LoadMapsAsync();
                RequestOpenMap?.Invoke(this, imported);
            }
        }

        private async void OnHelpClick(object sender, RoutedEventArgs e)
        {
            await ShowHelpDialogAsync();
        }

        private void OnCardMenuButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is MindMapMetadata meta)
            {
                var menu = new MenuFlyout();

                var itemOpen = new MenuFlyoutItem { Text = "Open Map", Icon = new FontIcon { Glyph = "" } };
                itemOpen.Click += async (s, args) =>
                {
                    var doc = await StorageService.Instance.LoadMapAsync(meta.Id);
                    if (doc != null) RequestOpenMap?.Invoke(this, doc);
                };
                menu.Items.Add(itemOpen);

                var itemRename = new MenuFlyoutItem { Text = "Rename", Icon = new FontIcon { Glyph = "" } };
                itemRename.Click += async (s, args) => await ShowRenameDialogAsync(meta);
                menu.Items.Add(itemRename);

                var itemChangeGroup = new MenuFlyoutItem { Text = "Change Group / Category", Icon = new FontIcon { Glyph = "" } };
                itemChangeGroup.Click += async (s, args) => await ShowChangeGroupDialogAsync(meta);
                menu.Items.Add(itemChangeGroup);

                var itemDuplicate = new MenuFlyoutItem { Text = "Duplicate", Icon = new FontIcon { Glyph = "" } };
                itemDuplicate.Click += async (s, args) => await _viewModel.DuplicateMapAsync(meta.Id);
                menu.Items.Add(itemDuplicate);

                menu.Items.Add(new MenuFlyoutSeparator());

                // Export Submenu
                var exportSub = new MenuFlyoutSubItem { Text = "Export As...", Icon = new FontIcon { Glyph = "" } };

                var expM8 = new MenuFlyoutItem { Text = "Quick MindMap (.m8map)" };
                expM8.Click += async (s, args) =>
                {
                    var doc = await StorageService.Instance.LoadMapAsync(meta.Id);
                    var win = (Application.Current as App)?.MainWindow;
                    if (doc != null && win != null) await ExportService.Instance.ExportAsJsonAsync(doc, win);
                };
                exportSub.Items.Add(expM8);

                var expMd = new MenuFlyoutItem { Text = "Markdown Outline (.md)" };
                expMd.Click += async (s, args) =>
                {
                    var doc = await StorageService.Instance.LoadMapAsync(meta.Id);
                    var win = (Application.Current as App)?.MainWindow;
                    if (doc != null && win != null) await ExportService.Instance.ExportAsMarkdownAsync(doc, win);
                };
                exportSub.Items.Add(expMd);

                var expOpml = new MenuFlyoutItem { Text = "OPML Outline (.opml)" };
                expOpml.Click += async (s, args) =>
                {
                    var doc = await StorageService.Instance.LoadMapAsync(meta.Id);
                    var win = (Application.Current as App)?.MainWindow;
                    if (doc != null && win != null) await ExportService.Instance.ExportAsOpmlAsync(doc, win);
                };
                exportSub.Items.Add(expOpml);

                var expTxt = new MenuFlyoutItem { Text = "Plain Text (.txt)" };
                expTxt.Click += async (s, args) =>
                {
                    var doc = await StorageService.Instance.LoadMapAsync(meta.Id);
                    var win = (Application.Current as App)?.MainWindow;
                    if (doc != null && win != null) await ExportService.Instance.ExportAsPlainTextAsync(doc, win);
                };
                exportSub.Items.Add(expTxt);

                menu.Items.Add(exportSub);

                menu.Items.Add(new MenuFlyoutSeparator());

                var itemDelete = new MenuFlyoutItem { Text = "Delete Map", Icon = new FontIcon { Glyph = "" } };
                itemDelete.Click += async (s, args) => await ShowDeleteConfirmDialogAsync(meta);
                menu.Items.Add(itemDelete);

                menu.ShowAt(btn);
            }
        }

        private async Task ShowNewMapDialogAsync()
        {
            var stack = new StackPanel { Spacing = 14, Width = 380 };

            var txtTitle = new TextBox { Header = "Map Title", Text = "New Mind Map", PlaceholderText = "e.g. Q4 Strategy, Biology Notes..." };
            var txtGroup = new TextBox { Header = "Category / Group", Text = "General", PlaceholderText = "e.g. Work, Study, Personal..." };

            var comboTemplate = new ComboBox { Header = "Template", HorizontalAlignment = HorizontalAlignment.Stretch };
            foreach (var t in MindMapTemplate.Templates)
            {
                comboTemplate.Items.Add(new ComboBoxItem { Content = $"{t.Icon} {t.Name}", Tag = t });
            }
            comboTemplate.SelectedIndex = 0;

            var comboPalette = new ComboBox { Header = "Color Palette", HorizontalAlignment = HorizontalAlignment.Stretch };
            foreach (var p in ColorPalette.AvailablePalettes)
            {
                comboPalette.Items.Add(new ComboBoxItem { Content = p.Name, Tag = p.Name });
            }
            comboPalette.SelectedIndex = 0;

            stack.Children.Add(txtTitle);
            stack.Children.Add(txtGroup);
            stack.Children.Add(comboTemplate);
            stack.Children.Add(comboPalette);

            var dialog = new ContentDialog
            {
                Title = "Create New Mind Map",
                Content = stack,
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var selectedItem = comboTemplate.SelectedItem as ComboBoxItem;
                var template = selectedItem?.Tag as MindMapTemplate ?? MindMapTemplate.Templates[0];

                var paletteItem = comboPalette.SelectedItem as ComboBoxItem;
                string palette = paletteItem?.Tag as string ?? "Vivid Spectrum";

                string title = string.IsNullOrWhiteSpace(txtTitle.Text) ? "New Mind Map" : txtTitle.Text.Trim();
                string group = string.IsNullOrWhiteSpace(txtGroup.Text) ? "General" : txtGroup.Text.Trim();

                var doc = await _viewModel.CreateMapFromTemplateAsync(template, title, group);
                doc.ThemePalette = palette;
                await StorageService.Instance.SaveMapAsync(doc);
                RequestOpenMap?.Invoke(this, doc);
            }
        }

        private async Task ShowRenameDialogAsync(MindMapMetadata meta)
        {
            var txt = new TextBox { Text = meta.Title, Header = "Map Title", HorizontalAlignment = HorizontalAlignment.Stretch };
            var dialog = new ContentDialog
            {
                Title = "Rename Mind Map",
                Content = txt,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(txt.Text))
            {
                await _viewModel.RenameMapAsync(meta.Id, txt.Text.Trim());
            }
        }

        private async Task ShowChangeGroupDialogAsync(MindMapMetadata meta)
        {
            var txt = new TextBox { Text = meta.Group, Header = "Category / Group", HorizontalAlignment = HorizontalAlignment.Stretch };
            var dialog = new ContentDialog
            {
                Title = "Change Group",
                Content = txt,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(txt.Text))
            {
                await _viewModel.UpdateGroupAsync(meta.Id, txt.Text.Trim());
            }
        }

        private async Task ShowDeleteConfirmDialogAsync(MindMapMetadata meta)
        {
            var dialog = new ContentDialog
            {
                Title = "Delete Mind Map?",
                Content = $"Are you sure you want to delete '{meta.Title}'? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await _viewModel.DeleteMapAsync(meta.Id);
            }
        }

        private async Task ShowHelpDialogAsync()
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
