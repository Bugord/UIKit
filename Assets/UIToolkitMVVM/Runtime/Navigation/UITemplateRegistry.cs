using System;
using System.Collections.Generic;
using UIToolkitMVVM.Widgets;
using UnityEngine;
using UnityEngine.UIElements;
using UIToolkitMVVM.Utils;

namespace UIToolkitMVVM.Navigation
{
    [CreateAssetMenu(menuName = "UI/Screen Registry")]
    public class UITemplateRegistry : ScriptableObject, IUIScreenRegistry, IWidgetTemplateRegistry
    {
        [Serializable]
        public class Entry
        {
            public string screenId;
            public VisualTreeAsset uxml;
        }

        public List<Entry> entries = new();
        public List<Entry> widgetEntries = new();

        private Dictionary<string, Entry> lookup;
        private Dictionary<string, Entry> widgetLookup;

        VisualTreeAsset IUIScreenRegistry.Get(string screenId)
        {
            lookup ??= entries.ToDictionaryChecked(e => e.screenId);

            if (lookup.TryGetValue(screenId, out var entry)) {
                return entry.uxml;
            }

            throw new KeyNotFoundException($"Screen {screenId} not found");
        }

        VisualTreeAsset IWidgetTemplateRegistry.Get(string widgetId)
        {
            widgetLookup ??= widgetEntries.ToDictionaryChecked(e => e.screenId);

            if (widgetLookup.TryGetValue(widgetId, out var entry)) {
                return entry.uxml;
            }

            throw new KeyNotFoundException($"Widget {widgetId} not found");
        }

        private void OnValidate()
        {
            lookup = null;
            widgetLookup = null;
        }
    }
}
