using System;
using System.Collections.Generic;

namespace UIKit.Editor
{
    // A snapshot of one catalog, so planning and validation run without assets.
    internal sealed class CatalogState
    {
        public readonly string Name;
        public readonly IReadOnlyList<string> Assemblies;
        public readonly IReadOnlyList<ScreenCatalog.Entry> Entries;

        public CatalogState(string name, IReadOnlyList<string> assemblies, IReadOnlyList<ScreenCatalog.Entry> entries)
        {
            Name = name;
            Assemblies = assemblies ?? Array.Empty<string>();
            Entries = entries ?? Array.Empty<ScreenCatalog.Entry>();
        }

        public static CatalogState Of(ScreenCatalog catalog) =>
            new(catalog.name, catalog.Assemblies, catalog.Entries);
    }
}
