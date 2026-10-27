using System.Collections.Generic;

namespace UIKit.Editor
{
    // The entries one catalog should hold after a sync.
    internal sealed class CatalogPlan
    {
        public readonly IReadOnlyList<ScreenCatalog.Entry> Entries;
        public readonly bool Changed;

        public CatalogPlan(IReadOnlyList<ScreenCatalog.Entry> entries, bool changed)
        {
            Entries = entries;
            Changed = changed;
        }
    }
}
