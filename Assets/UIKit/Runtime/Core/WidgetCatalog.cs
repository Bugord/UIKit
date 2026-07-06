using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIKit.Runtime.Core
{
    [CreateAssetMenu(menuName = "UIKit/Widget Catalog", fileName = "WidgetCatalog")]
    public class WidgetCatalog : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string widgetId;
            public VisualTreeAsset uxml;
        }

        [SerializeField]
        private Entry[] entries = Array.Empty<Entry>();

        private Dictionary<string, VisualTreeAsset> lookup;

        public VisualTreeAsset Get(string widgetId)
        {
            lookup ??= BuildLookup();

            return lookup.GetValueOrDefault(widgetId)
                   ?? throw new InvalidOperationException(
                       $"WidgetCatalog: no template for widget '{widgetId}'");
        }

        private Dictionary<string, VisualTreeAsset> BuildLookup()
        {
            var result = new Dictionary<string, VisualTreeAsset>(entries.Length);

            foreach (var entry in entries) {
                if (string.IsNullOrEmpty(entry.widgetId) || entry.uxml == null) {
                    continue;
                }

                if (!result.TryAdd(entry.widgetId, entry.uxml)) {
                    Debug.LogError($"WidgetCatalog: duplicate entry '{entry.widgetId}'", this);
                }
            }

            return result;
        }

#if UNITY_EDITOR
        public IReadOnlyList<Entry> Entries => entries;

        public void SetEntries(Entry[] newEntries)
        {
            entries = newEntries;
            lookup = null;
        }

        private void OnValidate()
        {
            lookup = null;
        }
#endif
    }
}