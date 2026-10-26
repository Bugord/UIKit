using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UIKit
{
    // The catalogs a context was created with, merged into one lookup.
    internal sealed class CatalogSet
    {
        private readonly Dictionary<string, (VisualTreeAsset uxml, ScreenCatalog catalog)> lookup = new();

        public CatalogSet(IEnumerable<ScreenCatalog> catalogs)
        {
            if (catalogs == null) {
                return;
            }

            foreach (var catalog in catalogs) {
                if (catalog == null) {
                    throw new ArgumentException("A screen catalog is null.", nameof(catalogs));
                }

                foreach (var entry in catalog.Entries) {
                    if (string.IsNullOrEmpty(entry.screenId)) {
                        continue;
                    }

                    if (lookup.TryGetValue(entry.screenId, out var existing)) {
                        if (existing.catalog == catalog) {
                            throw new InvalidOperationException(
                                $"{nameof(ScreenCatalog)}: screen '{entry.screenId}' is listed twice in '{catalog.name}'.");
                        }

                        // Which UXML wins would otherwise depend on the order catalogs were passed in.
                        throw new InvalidOperationException(
                            $"{nameof(ScreenCatalog)}: screen '{entry.screenId}' is listed in both " +
                            $"'{existing.catalog.name}' and '{catalog.name}'. A screen belongs to one catalog.");
                    }

                    lookup.Add(entry.screenId, (entry.uxml, catalog));
                }
            }
        }

        public VisualTreeAsset GetUxml(string screenId)
        {
            if (!lookup.TryGetValue(screenId, out var found)) {
                throw new InvalidOperationException(
                    $"{nameof(ScreenCatalog)}: no catalog lists screen '{screenId}'. Sync the catalogs " +
                    "via Project Settings > UI Kit, and check the catalog holding it was passed to the " +
                    $"{nameof(UIKitContext)}.");
            }

            if (found.uxml == null) {
                throw new InvalidOperationException(
                    $"{nameof(ScreenCatalog)}: no UXML assigned for screen '{screenId}' in '{found.catalog.name}'. " +
                    $"Name the asset '{screenId}.uxml' and press \"Sync with code\", or assign it in " +
                    "Project Settings > UI Kit.");
            }

            return found.uxml;
        }
    }
}
