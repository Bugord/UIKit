using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UIKit.Runtime.Core;
using UIKit.Runtime.Utils;
using UnityEditor;

namespace UIKit.Editor
{
    public static class WidgetRegistrySync
    {
        public static SyncReport Sync(WidgetCatalog catalog)
        {
            var report = new SyncReport();

            var widgetTypes = TypeCache.GetTypesDerivedFrom<IWidget>()
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
                .Where(t => t.GetCustomAttribute<UIWidgetAttribute>()?.Templated == true)
                .ToList();

            var wanted = new Dictionary<string, Type>();
            foreach (var type in widgetTypes) {
                var id = UIId.OfWidget(type);
                if (!wanted.TryAdd(id, type)) {
                    report.DuplicateIds.Add(id);
                }
            }

            var existing = catalog.Entries.ToDictionary(e => e.widgetId);
            var result = new List<WidgetCatalog.Entry>();

            foreach (var id in wanted.Keys) {
                if (!existing.TryGetValue(id, out var entry)) {
                    entry = new WidgetCatalog.Entry { widgetId = id };
                    report.Added.Add(id);
                }

                if (entry.uxml == null) {
                    entry.uxml = UIRegistrySync.FindUxmlByName(id);
                    if (entry.uxml == null) {
                        report.MissingUxml.Add(id);
                    }
                }

                result.Add(entry);
            }

            foreach (var id in existing.Keys.Except(wanted.Keys)) {
                report.Removed.Add(id);
            }

            catalog.SetEntries(result.OrderBy(e => e.widgetId).ToArray());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);

            report.Log(nameof(WidgetCatalog));
            return report;
        }
    }
}