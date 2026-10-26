using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIKit
{
    /// <summary>
    /// Maps screen ids to their UXML. Kept in sync with the code by Project Settings > UI Kit.
    /// </summary>
    /// <remarks>
    /// A project may have several catalogs, e.g. one per feature. A catalog lists the
    /// assemblies whose views it holds; the catalog with an empty list takes the views of every
    /// assembly no other catalog claims.
    /// </remarks>
    [CreateAssetMenu(menuName = "UIKit/Screen Catalog", fileName = "ScreenCatalog")]
    public class ScreenCatalog : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string screenId;
            public VisualTreeAsset uxml;
        }

        [Tooltip("Assemblies whose views this catalog holds. Leave empty for the default catalog, " +
                 "which holds the views of every assembly no other catalog claims.")]
        [SerializeField] private string[] assemblies = Array.Empty<string>();

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        public IReadOnlyList<string> Assemblies => assemblies;
        public IReadOnlyList<Entry> Entries => entries;

        public bool TryGetUxml(string screenId, out VisualTreeAsset uxml)
        {
            foreach (var entry in entries) {
                if (entry.screenId == screenId) {
                    uxml = entry.uxml;
                    return true;
                }
            }

            uxml = null;
            return false;
        }

        internal void SetEntries(Entry[] newEntries)
        {
            entries = newEntries ?? Array.Empty<Entry>();
        }

        internal void SetAssemblies(string[] newAssemblies)
        {
            assemblies = newAssemblies ?? Array.Empty<string>();
        }
    }
}
