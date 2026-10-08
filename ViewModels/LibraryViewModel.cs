using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using QuickMindMap.Helpers;
using QuickMindMap.Models;
using QuickMindMap.Services;

namespace QuickMindMap.ViewModels
{
    public class MapGroupItem
    {
        public string Header { get; set; } = string.Empty;
        public ObservableCollection<MindMapMetadata> Items { get; set; } = new();
    }

    public class LibraryViewModel : ViewModelBase
    {
        private List<MindMapMetadata> _allMaps = new();
        private ObservableCollection<MapGroupItem> _groupedMaps = new();
        private string _searchText = string.Empty;
        private string _filterMode = "Name"; // "Name", "Group", "Recent"
        private bool _isEmpty = true;
        private bool _isLoading;
        private int _totalCount;

        public ObservableCollection<MapGroupItem> GroupedMaps
        {
            get => _groupedMaps;
            set => SetProperty(ref _groupedMaps, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyGroupingAndFilter();
                }
            }
        }

        public string FilterMode
        {
            get => _filterMode;
            set
            {
                if (SetProperty(ref _filterMode, value))
                {
                    ApplyGroupingAndFilter();
                }
            }
        }

        public bool IsEmpty
        {
            get => _isEmpty;
            set => SetProperty(ref _isEmpty, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        public List<MindMapTemplate> AvailableTemplates => MindMapTemplate.Templates;

        public async Task LoadMapsAsync()
        {
            try
            {
                IsLoading = true;
                _allMaps = await StorageService.Instance.GetAllMapsAsync();
                TotalCount = _allMaps.Count;
                IsEmpty = _allMaps.Count == 0;
                ApplyGroupingAndFilter();
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Failed to load maps in ViewModel", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void ApplyGroupingAndFilter()
        {
            try
            {
                var query = _allMaps.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    string term = SearchText.Trim().ToLowerInvariant();
                    query = query.Where(m =>
                        (m.Title != null && m.Title.ToLowerInvariant().Contains(term)) ||
                        (m.Group != null && m.Group.ToLowerInvariant().Contains(term)) ||
                        (m.PreviewTopics != null && m.PreviewTopics.Any(t => t.ToLowerInvariant().Contains(term))));
                }

                var filteredList = query.ToList();
                var groups = new ObservableCollection<MapGroupItem>();

                if (FilterMode == "Group")
                {
                    var byCategory = filteredList
                        .GroupBy(m => string.IsNullOrWhiteSpace(m.Group) ? "General" : m.Group.Trim())
                        .OrderBy(g => g.Key);

                    foreach (var grp in byCategory)
                    {
                        var groupItem = new MapGroupItem
                        {
                            Header = grp.Key,
                            Items = new ObservableCollection<MindMapMetadata>(grp.OrderByDescending(m => m.ModifiedAt))
                        };
                        groups.Add(groupItem);
                    }
                }
                else if (FilterMode == "Recent")
                {
                    var recentItems = filteredList.OrderByDescending(m => m.ModifiedAt).ToList();
                    if (recentItems.Count > 0)
                    {
                        groups.Add(new MapGroupItem
                        {
                            Header = "Recently Modified",
                            Items = new ObservableCollection<MindMapMetadata>(recentItems)
                        });
                    }
                }
                else // "Name" - Alphabetical grouping
                {
                    var byLetter = filteredList
                        .GroupBy(m => m.FirstLetter)
                        .OrderBy(g => g.Key == "#" ? "ZZZ" : g.Key);

                    foreach (var grp in byLetter)
                    {
                        var groupItem = new MapGroupItem
                        {
                            Header = grp.Key,
                            Items = new ObservableCollection<MindMapMetadata>(grp.OrderBy(m => m.Title))
                        };
                        groups.Add(groupItem);
                    }
                }

                GroupedMaps = groups;
                IsEmpty = _allMaps.Count == 0;
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Error applying grouping and filter", ex);
            }
        }

        public async Task<MindMapDocument> CreateMapFromTemplateAsync(MindMapTemplate template, string? customTitle = null, string? customGroup = null)
        {
            var doc = template.Factory();
            if (!string.IsNullOrWhiteSpace(customTitle))
            {
                doc.Title = customTitle.Trim();
                if (doc.RootNode != null)
                {
                    doc.RootNode.Text = customTitle.Trim();
                }
            }
            if (!string.IsNullOrWhiteSpace(customGroup))
            {
                doc.Group = customGroup.Trim();
            }

            LayoutEngine.ApplyAutoLayout(doc);
            await StorageService.Instance.SaveMapAsync(doc);
            await LoadMapsAsync();
            return doc;
        }

        public async Task<bool> DeleteMapAsync(string id)
        {
            bool ok = await StorageService.Instance.DeleteMapAsync(id);
            if (ok)
            {
                await LoadMapsAsync();
            }
            return ok;
        }

        public async Task<MindMapDocument?> DuplicateMapAsync(string id)
        {
            var copy = await StorageService.Instance.DuplicateMapAsync(id);
            if (copy != null)
            {
                await LoadMapsAsync();
            }
            return copy;
        }

        public async Task<bool> RenameMapAsync(string id, string newTitle)
        {
            bool ok = await StorageService.Instance.RenameMapAsync(id, newTitle);
            if (ok)
            {
                await LoadMapsAsync();
            }
            return ok;
        }

        public async Task<bool> UpdateGroupAsync(string id, string newGroup)
        {
            bool ok = await StorageService.Instance.UpdateMapGroupAsync(id, newGroup);
            if (ok)
            {
                await LoadMapsAsync();
            }
            return ok;
        }
    }
}
