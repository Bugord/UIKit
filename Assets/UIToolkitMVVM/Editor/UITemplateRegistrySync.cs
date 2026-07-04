using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UIToolkitMVVM.Navigation;
using UIToolkitMVVM.Utils;
using UIToolkitMVVM.Views;
using UIToolkitMVVM.Widgets;
using UnityEditor;
using UnityEngine.UIElements;

namespace UIToolkitMVVM.Editor
{
    public static class UITemplateRegistrySync
    {
        public readonly struct SyncResult
        {
            public readonly int Added;
            public readonly IReadOnlyList<string> Warnings;

            public SyncResult(int added, IReadOnlyList<string> warnings)
            {
                Added = added;
                Warnings = warnings;
            }
        }

        public static SyncResult SyncScreens(UITemplateRegistry registry)
        {
            return Sync(
                TypeCache.GetTypesDerivedFrom<IView>().Where(t => !typeof(ILoadableWidget).IsAssignableFrom(t)),
                UIIdConventions.ResolveScreenId,
                registry.entries,
                "Screen");
        }

        public static SyncResult SyncWidgets(UITemplateRegistry registry)
        {
            return Sync(
                TypeCache.GetTypesDerivedFrom<ILoadableWidget>(),
                UIIdConventions.ResolveWidgetId,
                registry.widgetEntries,
                "Widget");
        }

        private static SyncResult Sync(
            IEnumerable<Type> candidates,
            Func<Type, string> resolveId,
            List<UITemplateRegistry.Entry> entries,
            string kind)
        {
            var concreteTypes = candidates.Where(t => !t.IsAbstract && !t.IsInterface).ToList();
            var existingIds = new HashSet<string>(entries.Select(e => e.id));
            var warnings = new List<string>();
            var added = 0;

            foreach (var type in concreteTypes) {
                var id = resolveId(type);
                if (existingIds.Contains(id)) continue;

                var matches = FindCandidateUxmlPaths(type.Name, id);

                if (matches.Count == 1) {
                    var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(matches[0]);
                    entries.Add(new UITemplateRegistry.Entry { id = id, uxml = uxml });
                    existingIds.Add(id);
                    added++;
                } else if (matches.Count == 0) {
                    warnings.Add($"{kind} '{type.Name}' (id \"{id}\"): no matching UXML found, add it manually.");
                } else {
                    warnings.Add($"{kind} '{type.Name}' (id \"{id}\"): {matches.Count} matching UXML files found, add it manually.");
                }
            }

            var liveIds = new HashSet<string>(concreteTypes.Select(resolveId));
            foreach (var entry in entries) {
                if (!liveIds.Contains(entry.id)) {
                    warnings.Add($"{kind} entry \"{entry.id}\" has no matching type in the project (renamed or removed?).");
                }
            }

            return new SyncResult(added, warnings);
        }

        private static List<string> FindCandidateUxmlPaths(string typeName, string id)
        {
            return AssetDatabase.FindAssets("t:VisualTreeAsset")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => {
                    var fileName = Path.GetFileNameWithoutExtension(path);
                    return fileName.Equals(typeName, StringComparison.OrdinalIgnoreCase)
                        || fileName.Equals(id, StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
        }
    }
}
