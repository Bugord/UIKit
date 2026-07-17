using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UIKit.Runtime.Abstractions;
using UIKit.Runtime.Core;
using UIKit.Runtime.Utils;
using UnityEditor;
using UnityEngine.UIElements;

namespace UIKit.Editor
{
    public static class UIRegistrySync
    {
        public static SyncReport Sync(ScreenCatalog catalog)
        {
            var report = new SyncReport();

            var viewModelTypes = TypeCache.GetTypesDerivedFrom<ViewModel>()
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
                .ToList();

            var wanted = new Dictionary<string, Type>();
            foreach (var type in viewModelTypes) {
                var id = UIId.OfScreen(type);
                if (!wanted.TryAdd(id, type)) {
                    report.DuplicateIds.Add(id);
                }
            }

            var existing = catalog.Entries.ToDictionary(e => e.screenId);
            var result = new List<ScreenCatalog.Entry>();

            foreach (var (id, vmType) in wanted) {
                if (!existing.TryGetValue(id, out var entry)) {
                    entry = new ScreenCatalog.Entry { screenId = id };
                    report.Added.Add(id);
                }

                entry.layer = ResolveLayer(vmType);
                entry.popupOptions = ResolvePopupOptions(vmType);

                if (entry.uxml == null) {
                    entry.uxml = FindUxmlByName(id);
                    if (entry.uxml == null) {
                        report.MissingUxml.Add(id);
                    }
                }

                result.Add(entry);
            }

            foreach (var id in existing.Keys.Except(wanted.Keys)) {
                report.Removed.Add(id);
            }

            catalog.SetEntries(result.OrderBy(e => e.screenId).ToArray());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);

            report.Log(nameof(ScreenCatalog));
            return report;
        }

        private static UILayer ResolveLayer(Type vmType)
        {
            var attr = vmType.GetCustomAttribute<UIScreenAttribute>();
            if (attr is { LayerSpecified: true }) {
                return attr.Layer;
            }

            return typeof(IPopup).IsAssignableFrom(vmType) ? UILayer.Popup : UILayer.Screen;
        }

        private static ScreenCatalog.PopupOptionsData ResolvePopupOptions(Type vmType)
        {
            var attr = vmType.GetCustomAttribute<UIScreenAttribute>();

            if (attr == null) {
                return ScreenCatalog.PopupOptionsData.Default;
            }

            return new ScreenCatalog.PopupOptionsData {
                cancelOnBackdropClick = attr.CancelOnBackdropClick,
                cancelOnNavigationCancel = attr.CancelOnNavigationCancel
            };
        }

        internal static VisualTreeAsset FindUxmlByName(string id)
        {
            var matches = AssetDatabase.FindAssets($"{id} t:VisualTreeAsset")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileNameWithoutExtension(p) == id)
                .ToList();

            return matches.Count == 1
                ? AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(matches[0])
                : null;
        }
    }
}