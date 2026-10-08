using System;
using Microsoft.UI.Xaml;
using QuickMindMap.Helpers;
using QuickMindMap.Models;

namespace QuickMindMap
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            try
            {
                WindowHelper.SetAppIcon(this);
                WindowHelper.Maximize(this);
            }
            catch (Exception ex)
            {
                AppLogger.LogException(ex, "MainWindow.ctor.WindowHelper");
            }

            // Wire screen navigation events
            MainLibraryView.RequestOpenMap += OnRequestOpenMap;
            MainEditorView.RequestBackToLibrary += OnRequestBackToLibrary;
        }

        private void OnRequestOpenMap(object? sender, MindMapDocument document)
        {
            try
            {
                MainLibraryView.Visibility = Visibility.Collapsed;
                MainEditorView.Visibility = Visibility.Visible;
                MainEditorView.LoadDocument(document);
                this.Title = $"{document.Title} — Quick MindMap";
            }
            catch (Exception ex)
            {
                AppLogger.LogException(ex, "OnRequestOpenMap");
            }
        }

        private async void OnRequestBackToLibrary(object? sender, EventArgs e)
        {
            try
            {
                MainEditorView.Visibility = Visibility.Collapsed;
                MainLibraryView.Visibility = Visibility.Visible;
                this.Title = "Quick MindMap";
                await MainLibraryView.ViewModel.LoadMapsAsync();
            }
            catch (Exception ex)
            {
                AppLogger.LogException(ex, "OnRequestBackToLibrary");
            }
        }
    }
}
