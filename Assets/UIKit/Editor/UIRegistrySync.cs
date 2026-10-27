using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace UIKit.Editor
{
    // Brings every screen catalog in line with the views in code.
    internal static class UIRegistrySync
    {
        public static SyncReport Sync() => Sync(CatalogLocator.FindAll());

        public static SyncReport Sync(IReadOnlyList<ScreenCatalog> catalogs)
        {
            var states = catalogs.Select(CatalogState.Of).ToList();
            var result = SyncPlanner.Plan(states, ScreenDiscovery.FindScreens(), FindUxmlByName);
            Apply(catalogs, result);
            return result.Report;
        }

        public static void Apply(IReadOnlyList<ScreenCatalog> catalogs, SyncResult result)
        {
            for (var i = 0; i < catalogs.Count; i++) {
                var plan = result.Catalogs[i];
                if (!plan.Changed) {
                    continue;
                }

                var catalog = catalogs[i];
                Undo.RecordObject(catalog, "Sync Screen Catalog");
                catalog.SetEntries(plan.Entries.ToArray());
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssetIfDirty(catalog);
            }
        }

        public static VisualTreeAsset FindUxmlByName(string screenId)
        {
            var matches = AssetDatabase.FindAssets($"{screenId} t:VisualTreeAsset")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileNameWithoutExtension(path) == screenId)
                .Distinct()
                .ToList();

            // Two files with the same name: picking one would be a guess, so leave it to the user.
            return matches.Count == 1
                ? AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(matches[0])
                : null;
        }
    }
}
