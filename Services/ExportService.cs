using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using QuickMindMap.Helpers;
using QuickMindMap.Models;
using WinRT.Interop;

namespace QuickMindMap.Services
{
    public class ExportService
    {
        private static ExportService? _instance;
        public static ExportService Instance => _instance ??= new ExportService();

        public async Task<bool> ExportAsPngAsync(FrameworkElement element, Window window, string suggestedFileName)
        {
            try
            {
                var savePicker = new FileSavePicker();
                InitializeWithWindow.Initialize(savePicker, WindowNative.GetWindowHandle(window));
                savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                savePicker.FileTypeChoices.Add("PNG Image", new[] { ".png" });
                savePicker.SuggestedFileName = SanitizeFileName(suggestedFileName);

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file == null) return false;

                var rtb = new RenderTargetBitmap();
                await rtb.RenderAsync(element);

                var pixelBuffer = await rtb.GetPixelsAsync();
                var pixels = pixelBuffer.ToArray();

                using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Premultiplied,
                        (uint)rtb.PixelWidth,
                        (uint)rtb.PixelHeight,
                        96.0,
                        96.0,
                        pixels);
                    await encoder.FlushAsync();
                }

                AppLogger.LogInfo($"Exported PNG image to {file.Path}");
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Export to PNG failed", ex);
                return false;
            }
        }

        public async Task<bool> ExportAsJsonAsync(MindMapDocument document, Window window)
        {
            try
            {
                var savePicker = new FileSavePicker();
                InitializeWithWindow.Initialize(savePicker, WindowNative.GetWindowHandle(window));
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("Quick MindMap (*.m8map)", new[] { ".m8map" });
                savePicker.FileTypeChoices.Add("JSON File (*.json)", new[] { ".json" });
                savePicker.SuggestedFileName = SanitizeFileName(document.Title);

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file == null) return false;

                string json = document.ToJson();
                await FileIO.WriteTextAsync(file, json);
                AppLogger.LogInfo($"Exported mind map to {file.Path}");
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Export to JSON failed", ex);
                return false;
            }
        }

        public async Task<bool> ExportAsMarkdownAsync(MindMapDocument document, Window window)
        {
            try
            {
                var savePicker = new FileSavePicker();
                InitializeWithWindow.Initialize(savePicker, WindowNative.GetWindowHandle(window));
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("Markdown Outline (*.md)", new[] { ".md" });
                savePicker.SuggestedFileName = SanitizeFileName(document.Title);

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file == null) return false;

                string md = StorageService.Instance.ExportToMarkdown(document);
                await FileIO.WriteTextAsync(file, md);
                AppLogger.LogInfo($"Exported Markdown to {file.Path}");
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Export to Markdown failed", ex);
                return false;
            }
        }

        public async Task<bool> ExportAsOpmlAsync(MindMapDocument document, Window window)
        {
            try
            {
                var savePicker = new FileSavePicker();
                InitializeWithWindow.Initialize(savePicker, WindowNative.GetWindowHandle(window));
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("OPML Outline (*.opml)", new[] { ".opml" });
                savePicker.SuggestedFileName = SanitizeFileName(document.Title);

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file == null) return false;

                string opml = StorageService.Instance.ExportToOPML(document);
                await FileIO.WriteTextAsync(file, opml);
                AppLogger.LogInfo($"Exported OPML to {file.Path}");
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Export to OPML failed", ex);
                return false;
            }
        }

        public async Task<bool> ExportAsPlainTextAsync(MindMapDocument document, Window window)
        {
            try
            {
                var savePicker = new FileSavePicker();
                InitializeWithWindow.Initialize(savePicker, WindowNative.GetWindowHandle(window));
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("Plain Text Outline (*.txt)", new[] { ".txt" });
                savePicker.SuggestedFileName = SanitizeFileName(document.Title);

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file == null) return false;

                string txt = StorageService.Instance.ExportToPlainText(document);
                await FileIO.WriteTextAsync(file, txt);
                AppLogger.LogInfo($"Exported Text Outline to {file.Path}");
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Export to Text failed", ex);
                return false;
            }
        }

        public async Task<MindMapDocument?> ImportMapFileAsync(Window window)
        {
            try
            {
                var openPicker = new FileOpenPicker();
                InitializeWithWindow.Initialize(openPicker, WindowNative.GetWindowHandle(window));
                openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                openPicker.FileTypeFilter.Add(".m8map");
                openPicker.FileTypeFilter.Add(".json");
                openPicker.FileTypeFilter.Add(".md");

                StorageFile file = await openPicker.PickSingleFileAsync();
                if (file == null) return null;

                string content = await FileIO.ReadTextAsync(file);

                if (file.FileType.Equals(".md", StringComparison.OrdinalIgnoreCase))
                {
                    return ImportMarkdownToDocument(content, file.DisplayName);
                }
                else
                {
                    var doc = MindMapDocument.FromJson(content);
                    if (doc != null)
                    {
                        if (string.IsNullOrWhiteSpace(doc.Id)) doc.Id = Guid.NewGuid().ToString("N");
                        await StorageService.Instance.SaveMapAsync(doc);
                        return doc;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Import file failed", ex);
            }
            return null;
        }

        private MindMapDocument ImportMarkdownToDocument(string markdown, string fileName)
        {
            var doc = MindMapDocument.CreateDefault(fileName, "Imported", "Vivid Spectrum");
            var pal = ColorPalette.GetPalette(doc.ThemePalette);
            var lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            var stack = new List<(int level, MindMapNode node)>();
            stack.Add((1, doc.RootNode));

            int branchIndex = 0;

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.StartsWith("#"))
                {
                    int level = 0;
                    while (level < line.Length && line[level] == '#') level++;

                    string title = line.Substring(level).Trim();
                    if (level == 1)
                    {
                        doc.Title = title;
                        doc.RootNode.Text = title;
                    }
                    else
                    {
                        while (stack.Count > 0 && stack[^1].level >= level)
                        {
                            stack.RemoveAt(stack.Count - 1);
                        }

                        var parent = stack.Count > 0 ? stack[^1].node : doc.RootNode;
                        string color = ColorPalette.GetColorForBranchIndex(branchIndex++, doc.ThemePalette);

                        var childNode = new MindMapNode
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            ParentId = parent.Id,
                            Text = title,
                            BranchColor = parent.IsRoot ? color : parent.BranchColor,
                            FontSize = parent.IsRoot ? 14 : 13,
                            Children = new()
                        };
                        parent.Children.Add(childNode);
                        stack.Add((level, childNode));
                    }
                }
                else if (line.StartsWith("- ") || line.StartsWith("* ") || line.StartsWith("• "))
                {
                    string itemText = line.Substring(2).Trim();
                    var parent = stack.Count > 0 ? stack[^1].node : doc.RootNode;
                    var childNode = new MindMapNode
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        ParentId = parent.Id,
                        Text = itemText,
                        BranchColor = parent.BranchColor,
                        FontSize = 14,
                        Children = new()
                    };
                    parent.Children.Add(childNode);
                }
                else if (line.StartsWith(">"))
                {
                    if (stack.Count > 0)
                    {
                        stack[^1].node.Note = line.Substring(1).Trim();
                    }
                }
            }

            return doc;
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "MindMap";
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name.Trim();
        }
    }
}
