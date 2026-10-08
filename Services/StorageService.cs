using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickMindMap.Helpers;
using QuickMindMap.Models;

namespace QuickMindMap.Services
{
    public class StorageService
    {
        private static StorageService? _instance;
        public static StorageService Instance => _instance ??= new StorageService();

        private string _mapsDirectory = string.Empty;

        public StorageService()
        {
            InitializeDirectory();
        }

        private void InitializeDirectory()
        {
            try
            {
                _mapsDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "QuickMindMap",
                    "MindMaps");

                if (!Directory.Exists(_mapsDirectory))
                {
                    Directory.CreateDirectory(_mapsDirectory);
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Failed to initialize storage directory", ex);
            }
        }

        public async Task<List<MindMapMetadata>> GetAllMapsAsync()
        {
            var list = new List<MindMapMetadata>();
            try
            {
                if (!Directory.Exists(_mapsDirectory))
                {
                    Directory.CreateDirectory(_mapsDirectory);
                }

                var files = Directory.GetFiles(_mapsDirectory, "*.m8map");
                if (files.Length == 0)
                {
                    await EnsureSampleMapsAsync();
                    files = Directory.GetFiles(_mapsDirectory, "*.m8map");
                }

                var seenDocs = new Dictionary<string, (MindMapDocument doc, string filePath)>(StringComparer.OrdinalIgnoreCase);

                foreach (var file in files)
                {
                    try
                    {
                        string json = await File.ReadAllTextAsync(file, Encoding.UTF8);
                        var doc = MindMapDocument.FromJson(json);
                        if (doc != null)
                        {
                            doc.FilePath = file;
                            string nameWithoutExt = Path.GetFileNameWithoutExtension(file);
                            if (!string.IsNullOrWhiteSpace(nameWithoutExt) && !Guid.TryParse(nameWithoutExt, out _))
                            {
                                doc.Id = nameWithoutExt;
                            }

                            if (doc.LayoutType != "Freeform" || (doc.RootNode?.Children.Any(c => c.X == 0 && c.Y == 0) ?? false))
                            {
                                LayoutEngine.ApplyAutoLayout(doc);
                                string updatedJson = doc.ToJson();
                                await File.WriteAllTextAsync(file, updatedJson, Encoding.UTF8);
                            }

                            string key = doc.Id;
                            if (seenDocs.TryGetValue(key, out var existing))
                            {
                                var currentInfo = new FileInfo(file);
                                var existingInfo = new FileInfo(existing.filePath);
                                if (currentInfo.LastWriteTimeUtc > existingInfo.LastWriteTimeUtc)
                                {
                                    try { File.Delete(existing.filePath); } catch { }
                                    seenDocs[key] = (doc, file);
                                }
                                else
                                {
                                    try { File.Delete(file); } catch { }
                                }
                            }
                            else
                            {
                                seenDocs[key] = (doc, file);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogWarning($"Failed to read map file {file}: {ex.Message}");
                    }
                }

                foreach (var kvp in seenDocs)
                {
                    list.Add(kvp.Value.doc.ToMetadata(kvp.Value.filePath));
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Error loading maps list", ex);
            }

            return list.OrderByDescending(m => m.ModifiedAt).ToList();
        }

        public async Task EnsureSampleMapsAsync()
        {
            try
            {
                if (!Directory.Exists(_mapsDirectory))
                {
                    Directory.CreateDirectory(_mapsDirectory);
                }

                // Copy from bundled Samples directory if available
                string appDir = AppContext.BaseDirectory;
                string projectSamples = Path.Combine(appDir, "Samples");
                if (!Directory.Exists(projectSamples))
                {
                    projectSamples = Path.Combine(Directory.GetCurrentDirectory(), "Samples");
                }

                if (Directory.Exists(projectSamples))
                {
                    var sampleFiles = Directory.GetFiles(projectSamples, "*.m8map");
                    if (sampleFiles.Length > 0)
                    {
                        foreach (var sf in sampleFiles)
                        {
                            string dest = Path.Combine(_mapsDirectory, Path.GetFileName(sf));
                            bool shouldCopy = !File.Exists(dest);
                            if (!shouldCopy)
                            {
                                try
                                {
                                    string existingJson = File.ReadAllText(dest, Encoding.UTF8);
                                    if (existingJson.Contains("\"x\":  0") || existingJson.Contains("\"x\": 0"))
                                    {
                                        shouldCopy = true;
                                    }
                                }
                                catch { }
                            }
                            if (shouldCopy)
                            {
                                File.Copy(sf, dest, true);
                            }
                        }
                        return;
                    }
                }

                // Clean up any legacy duplicate sample files
                var oldDuplicates = new[]
                {
                    "sample_project_planning.m8map",
                    "sample_study_and_exam_notes.m8map",
                    "sample_brainstorming_session.m8map",
                    "sample_project_plan.m8map",
                    "sample_study_notes.m8map",
                    "sample_brainstorming.m8map"
                };
                foreach (var oldName in oldDuplicates)
                {
                    string oldPath = Path.Combine(_mapsDirectory, oldName);
                    if (File.Exists(oldPath))
                    {
                        try { File.Delete(oldPath); } catch { }
                    }
                }

                // If not copied from Samples folder, generate programmatically from templates
                var templates = new[]
                {
                    (Id: "Product_Innovation_Brainstorming", TemplateId: "brainstorm", Title: "Product Innovation & Strategy", Group: "Brainstorming"),
                    (Id: "Q4_Product_Launch_Plan", TemplateId: "project_plan", Title: "Q4 Product Launch Plan", Group: "Work"),
                    (Id: "Computer_Science_System_Architecture", TemplateId: "study_notes", Title: "Computer Science & Systems", Group: "Study")
                };

                foreach (var item in templates)
                {
                    var tmpl = MindMapTemplate.Templates.FirstOrDefault(t => t.Id == item.TemplateId);
                    if (tmpl != null)
                    {
                        var doc = tmpl.Factory();
                        doc.Id = item.Id;
                        doc.Title = item.Title;
                        doc.Group = item.Group;
                        if (doc.RootNode != null) doc.RootNode.Text = item.Title;
                        LayoutEngine.ApplyAutoLayout(doc);
                        await SaveMapAsync(doc);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Failed to ensure sample maps", ex);
            }
        }

        public async Task<MindMapDocument?> LoadMapAsync(string id)
        {
            try
            {
                string filePath = Path.Combine(_mapsDirectory, $"{id}.m8map");
                if (!File.Exists(filePath))
                {
                    // Check if file path was passed directly
                    if (File.Exists(id))
                    {
                        filePath = id;
                    }
                    else
                    {
                        string altPath = Path.Combine(_mapsDirectory, id);
                        if (File.Exists(altPath))
                        {
                            filePath = altPath;
                        }
                        else
                        {
                            // Search all files in maps directory for matching ID or filename
                            var files = Directory.GetFiles(_mapsDirectory, "*.m8map");
                            var nameMatch = files.FirstOrDefault(f =>
                                Path.GetFileNameWithoutExtension(f).Equals(id, StringComparison.OrdinalIgnoreCase) ||
                                Path.GetFileName(f).Equals(id, StringComparison.OrdinalIgnoreCase));

                            if (nameMatch != null)
                            {
                                filePath = nameMatch;
                            }
                            else
                            {
                                foreach (var f in files)
                                {
                                    try
                                    {
                                        string text = await File.ReadAllTextAsync(f, Encoding.UTF8);
                                        if (text.Contains($"\"{id}\""))
                                        {
                                            filePath = f;
                                            break;
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                }

                if (!File.Exists(filePath)) return null;

                string json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                var doc = MindMapDocument.FromJson(json);
                if (doc != null)
                {
                    doc.FilePath = filePath;
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                    if (!string.IsNullOrWhiteSpace(nameWithoutExt) && !Guid.TryParse(nameWithoutExt, out _))
                    {
                        doc.Id = nameWithoutExt;
                    }

                    if (doc.LayoutType != "Freeform" || (doc.RootNode?.Children.Any(c => c.X == 0 && c.Y == 0) ?? false))
                    {
                        LayoutEngine.ApplyAutoLayout(doc);
                        string updatedJson = doc.ToJson();
                        await File.WriteAllTextAsync(filePath, updatedJson, Encoding.UTF8);
                    }
                }
                return doc;
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Error loading map {id}", ex);
                return null;
            }
        }

        public async Task<bool> SaveMapAsync(MindMapDocument document)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(document.Id))
                {
                    document.Id = Guid.NewGuid().ToString("N");
                }

                document.ModifiedAt = DateTimeOffset.Now;

                string filePath;
                if (!string.IsNullOrWhiteSpace(document.FilePath))
                {
                    filePath = document.FilePath;
                }
                else
                {
                    filePath = Path.Combine(_mapsDirectory, $"{document.Id}.m8map");
                    document.FilePath = filePath;
                }

                string json = document.ToJson();

                await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
                AppLogger.LogInfo($"Saved map {document.Title} ({document.Id}) to {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Failed to save map {document.Title}", ex);
                return false;
            }
        }

        public async Task<bool> DeleteMapAsync(string id)
        {
            try
            {
                string filePath = Path.Combine(_mapsDirectory, $"{id}.m8map");
                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                    AppLogger.LogInfo($"Deleted map file: {filePath}");
                    return true;
                }

                if (File.Exists(id))
                {
                    await Task.Run(() => File.Delete(id));
                    return true;
                }

                var files = Directory.GetFiles(_mapsDirectory, "*.m8map");
                foreach (var f in files)
                {
                    if (Path.GetFileNameWithoutExtension(f).Equals(id, StringComparison.OrdinalIgnoreCase))
                    {
                        await Task.Run(() => File.Delete(f));
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Failed to delete map {id}", ex);
                return false;
            }
        }

        public async Task<MindMapDocument?> DuplicateMapAsync(string id)
        {
            try
            {
                var original = await LoadMapAsync(id);
                if (original == null) return null;

                var copy = original.Clone();
                copy.FilePath = null;
                await SaveMapAsync(copy);
                return copy;
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Failed to duplicate map {id}", ex);
                return null;
            }
        }

        public async Task<bool> RenameMapAsync(string id, string newTitle)
        {
            try
            {
                var doc = await LoadMapAsync(id);
                if (doc == null) return false;

                doc.Title = newTitle;
                if (doc.RootNode != null && (doc.RootNode.Text == "Main Topic" || doc.RootNode.Text == doc.Title))
                {
                    doc.RootNode.Text = newTitle;
                }
                return await SaveMapAsync(doc);
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Failed to rename map {id}", ex);
                return false;
            }
        }

        public async Task<bool> UpdateMapGroupAsync(string id, string newGroup)
        {
            try
            {
                var doc = await LoadMapAsync(id);
                if (doc == null) return false;

                doc.Group = newGroup;
                return await SaveMapAsync(doc);
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Failed to update map group for {id}", ex);
                return false;
            }
        }

        // Export to Markdown Outline
        public string ExportToMarkdown(MindMapDocument doc)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {doc.Title}");
            sb.AppendLine();
            if (doc.RootNode != null)
            {
                if (!string.IsNullOrWhiteSpace(doc.RootNode.Note))
                {
                    sb.AppendLine($"> {doc.RootNode.Note}");
                    sb.AppendLine();
                }

                foreach (var child in doc.RootNode.Children)
                {
                    AppendMarkdownNode(sb, child, 2);
                }
            }
            return sb.ToString();
        }

        private void AppendMarkdownNode(StringBuilder sb, MindMapNode node, int level)
        {
            string prefix = new string('#', Math.Min(level, 6));
            if (level <= 4)
            {
                sb.AppendLine($"{prefix} {node.Text}");
            }
            else
            {
                string indent = new string(' ', (level - 4) * 2);
                sb.AppendLine($"{indent}- {node.Text}");
            }

            if (!string.IsNullOrWhiteSpace(node.Note))
            {
                sb.AppendLine($"  > {node.Note}");
            }

            foreach (var child in node.Children)
            {
                AppendMarkdownNode(sb, child, level + 1);
            }
            sb.AppendLine();
        }

        // Export to OPML XML
        public string ExportToOPML(MindMapDocument doc)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<opml version=\"2.0\">");
            sb.AppendLine("  <head>");
            sb.AppendLine($"    <title>{EscapeXml(doc.Title)}</title>");
            sb.AppendLine($"    <dateCreated>{doc.CreatedAt:R}</dateCreated>");
            sb.AppendLine($"    <dateModified>{doc.ModifiedAt:R}</dateModified>");
            sb.AppendLine("  </head>");
            sb.AppendLine("  <body>");

            if (doc.RootNode != null)
            {
                AppendOpmlNode(sb, doc.RootNode, "    ");
            }

            sb.AppendLine("  </body>");
            sb.AppendLine("</opml>");
            return sb.ToString();
        }

        private void AppendOpmlNode(StringBuilder sb, MindMapNode node, string indent)
        {
            string noteAttr = !string.IsNullOrWhiteSpace(node.Note) ? $" _note=\"{EscapeXml(node.Note)}\"" : "";
            if (node.Children.Count == 0)
            {
                sb.AppendLine($"{indent}<outline text=\"{EscapeXml(node.Text)}\"{noteAttr} />");
            }
            else
            {
                sb.AppendLine($"{indent}<outline text=\"{EscapeXml(node.Text)}\"{noteAttr}>");
                foreach (var child in node.Children)
                {
                    AppendOpmlNode(sb, child, indent + "  ");
                }
                sb.AppendLine($"{indent}</outline>");
            }
        }

        // Export to Plain Text Indented Outline
        public string ExportToPlainText(MindMapDocument doc)
        {
            var sb = new StringBuilder();
            sb.AppendLine(doc.Title);
            sb.AppendLine(new string('=', doc.Title.Length));
            if (doc.RootNode != null)
            {
                foreach (var child in doc.RootNode.Children)
                {
                    AppendPlainTextNode(sb, child, 0);
                }
            }
            return sb.ToString();
        }

        private void AppendPlainTextNode(StringBuilder sb, MindMapNode node, int level)
        {
            string indent = new string(' ', level * 4);
            sb.AppendLine($"{indent}• {node.Text}");
            if (!string.IsNullOrWhiteSpace(node.Note))
            {
                sb.AppendLine($"{indent}  [Note: {node.Note}]");
            }
            foreach (var child in node.Children)
            {
                AppendPlainTextNode(sb, child, level + 1);
            }
        }

        private static string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("&", "&amp;")
                       .Replace("<", "&lt;")
                       .Replace(">", "&gt;")
                       .Replace("\"", "&quot;")
                       .Replace("'", "&apos;");
        }
    }
}
