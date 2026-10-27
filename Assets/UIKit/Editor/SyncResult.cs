using System.Collections.Generic;
using System.Linq;

namespace UIKit.Editor
{
    internal sealed class SyncResult
    {
        // Same order as the catalogs passed to SyncPlanner.Plan.
        public readonly IReadOnlyList<CatalogPlan> Catalogs;
        public readonly SyncReport Report;

        public bool HasChanges => Catalogs.Any(plan => plan.Changed);

        public SyncResult(IReadOnlyList<CatalogPlan> catalogs, SyncReport report)
        {
            Catalogs = catalogs;
            Report = report;
        }
    }
}
