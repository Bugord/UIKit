using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace UIKit.Editor
{
    internal static class CatalogLocator
    {
        // Sorted by path so sync and validation see the catalogs in a stable order.
        public static List<ScreenCatalog> FindAll()
        {
            return AssetDatabase.FindAssets($"t:{nameof(ScreenCatalog)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<ScreenCatalog>)
                .Where(catalog => catalog != null)
                .ToList();
        }
    }
}
