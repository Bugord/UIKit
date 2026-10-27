using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UIKit.Editor
{
    // Decides what every catalog should hold, given the screens in code. Touches no assets,
    // so it can be tested with fake data.
    internal static class SyncPlanner
    {
        public static SyncResult Plan(IReadOnlyList<CatalogState> catalogs, IReadOnlyList<ScreenSource> screens,
            Func<string, VisualTreeAsset> findUxml)
        {
            var report = new SyncReport();

            var claims = ClaimAssemblies(catalogs, report);
            var defaultIndex = FindDefaultCatalog(catalogs, report);

            var wanted = new List<string>[catalogs.Count];
            for (var i = 0; i < wanted.Length; i++) {
                wanted[i] = new List<string>();
            }

            var byId = screens
                .Where(screen => !string.IsNullOrEmpty(screen.ScreenId))
                .GroupBy(screen => screen.ScreenId)
                .OrderBy(group => group.Key, StringComparer.Ordinal);

            foreach (var group in byId) {
                var sources = group.OrderBy(screen => screen.TypeName, StringComparer.Ordinal).ToList();
                if (sources.Count > 1) {
                    report.DuplicateIds.Add((group.Key, sources.Select(screen => screen.TypeName).ToArray()));
                }

                var screen = sources[0];
                var index = screen.AssemblyName != null && claims.TryGetValue(screen.AssemblyName, out var claimed)
                    ? claimed
                    : defaultIndex;

                if (index < 0) {
                    report.Unassigned.Add((screen.ScreenId, screen.AssemblyName));
                    continue;
                }

                wanted[index].Add(screen.ScreenId);
            }

            // A screen that moves to another catalog keeps the UXML someone assigned by hand.
            var assignedAnywhere = new Dictionary<string, VisualTreeAsset>();
            foreach (var catalog in catalogs) {
                foreach (var entry in catalog.Entries) {
                    if (entry != null && !string.IsNullOrEmpty(entry.screenId) && entry.uxml != null) {
                        assignedAnywhere.TryAdd(entry.screenId, entry.uxml);
                    }
                }
            }

            var plans = new CatalogPlan[catalogs.Count];
            for (var i = 0; i < catalogs.Count; i++) {
                plans[i] = PlanCatalog(catalogs[i], wanted[i], assignedAnywhere, findUxml, report);
            }

            return new SyncResult(plans, report);
        }

        private static CatalogPlan PlanCatalog(CatalogState catalog, List<string> wantedIds,
            Dictionary<string, VisualTreeAsset> assignedAnywhere, Func<string, VisualTreeAsset> findUxml,
            SyncReport report)
        {
            var own = new Dictionary<string, VisualTreeAsset>();
            foreach (var entry in catalog.Entries) {
                if (entry == null || string.IsNullOrEmpty(entry.screenId)) {
                    continue;
                }

                // An id listed twice keeps the first entry that has UXML.
                if (!own.TryGetValue(entry.screenId, out var uxml) || uxml == null) {
                    own[entry.screenId] = entry.uxml;
                }
            }

            var entries = new List<ScreenCatalog.Entry>(wantedIds.Count);
            foreach (var id in wantedIds) {
                if (!own.TryGetValue(id, out var uxml)) {
                    report.Added.Add((catalog.Name, id));
                }

                if (uxml == null) {
                    assignedAnywhere.TryGetValue(id, out uxml);
                }
                if (uxml == null) {
                    uxml = findUxml?.Invoke(id);
                }
                if (uxml == null) {
                    report.MissingUxml.Add((catalog.Name, id));
                }

                entries.Add(new ScreenCatalog.Entry { screenId = id, uxml = uxml });
            }

            var wantedSet = new HashSet<string>(wantedIds);
            foreach (var id in own.Keys.Where(id => !wantedSet.Contains(id)).OrderBy(id => id, StringComparer.Ordinal)) {
                report.Removed.Add((catalog.Name, id));
            }

            return new CatalogPlan(entries, !SameEntries(catalog.Entries, entries));
        }

        private static Dictionary<string, int> ClaimAssemblies(IReadOnlyList<CatalogState> catalogs, SyncReport report)
        {
            var claims = new Dictionary<string, int>();
            var claimants = new Dictionary<string, List<string>>();

            for (var i = 0; i < catalogs.Count; i++) {
                foreach (var assembly in ListedAssemblies(catalogs[i])) {
                    claims.TryAdd(assembly, i);

                    if (!claimants.TryGetValue(assembly, out var names)) {
                        claimants[assembly] = names = new List<string>();
                    }
                    names.Add(catalogs[i].Name);
                }
            }

            foreach (var (assembly, names) in claimants.OrderBy(pair => pair.Key, StringComparer.Ordinal)) {
                if (names.Count > 1) {
                    report.ContestedAssemblies.Add((assembly, names.ToArray()));
                }
            }

            return claims;
        }

        private static int FindDefaultCatalog(IReadOnlyList<CatalogState> catalogs, SyncReport report)
        {
            var defaults = Enumerable.Range(0, catalogs.Count)
                .Where(i => !ListedAssemblies(catalogs[i]).Any())
                .ToList();

            if (defaults.Count > 1) {
                report.DefaultCatalogs.AddRange(defaults.Select(i => catalogs[i].Name));
            }

            return defaults.Count > 0 ? defaults[0] : -1;
        }

        // Blank rows are what the list editor leaves behind; they claim nothing.
        private static IEnumerable<string> ListedAssemblies(CatalogState catalog) =>
            catalog.Assemblies
                .Where(assembly => !string.IsNullOrWhiteSpace(assembly))
                .Select(assembly => assembly.Trim())
                .Distinct();

        private static bool SameEntries(IReadOnlyList<ScreenCatalog.Entry> current, IReadOnlyList<ScreenCatalog.Entry> planned)
        {
            if (current.Count != planned.Count) {
                return false;
            }

            for (var i = 0; i < current.Count; i++) {
                if (current[i] == null || current[i].screenId != planned[i].screenId || current[i].uxml != planned[i].uxml) {
                    return false;
                }
            }

            return true;
        }
    }
}
