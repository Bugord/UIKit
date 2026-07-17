using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIKit.Runtime.Core
{
    [CreateAssetMenu(menuName = "UIKit/Screen Catalog", fileName = "ScreenCatalog")]
    public class ScreenCatalog : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string screenId;
            public VisualTreeAsset uxml;
            public UILayer layer = UILayer.Screen;
            public PopupOptionsData popupOptions = PopupOptionsData.Default;
        }
 
        [Serializable]
        public struct PopupOptionsData
        {
            public bool cancelOnBackdropClick;
            public bool cancelOnNavigationCancel;
 
            public static PopupOptionsData Default => new() {
                cancelOnBackdropClick = true,
                cancelOnNavigationCancel = true
            };
 
            public PopupOptions ToOptions() => new() {
                CancelOnBackdropClick = cancelOnBackdropClick,
                CancelOnNavigationCancel = cancelOnNavigationCancel
            };
        }
 
        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
 
        private Dictionary<string, Entry> lookup;
 
        public Entry Get(string screenId)
        {
            lookup ??= BuildLookup();

            if (!lookup.TryGetValue(screenId, out var entry)) {
                throw new InvalidOperationException(
                    $"ScreenCatalog: no entry registered for screen id '{screenId}'. " +
                    "Sync the catalog via Project Settings > UI Kit > Sync with code.");
            }

            return entry;
        }
 
        private Dictionary<string, Entry> BuildLookup()
        {
            var result = new Dictionary<string, Entry>(entries.Length);
 
            foreach (var entry in entries) {
                if (string.IsNullOrEmpty(entry.screenId)) {
                    continue;
                }
 
                if (!result.TryAdd(entry.screenId, entry)) {
                    Debug.LogError($"ScreenCatalog: duplicate entry '{entry.screenId}'", this);
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