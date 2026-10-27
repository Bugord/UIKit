using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UIKit.Editor
{
    internal sealed class SyncReport
    {
        public readonly List<(string catalog, string screenId)> Added = new();
        public readonly List<(string catalog, string screenId)> Removed = new();
        public readonly List<(string catalog, string screenId)> MissingUxml = new();

        // Two view types resolve to the same screen id; only the first (by type name) is synced.
        public readonly List<(string screenId, string[] types)> DuplicateIds = new();

        // No catalog claims the assembly and there is no default catalog.
        public readonly List<(string screenId, string assembly)> Unassigned = new();

        // Filled only when there is more than one; the first takes the unclaimed screens.
        public readonly List<string> DefaultCatalogs = new();

        // An assembly listed by several catalogs; the first takes its screens.
        public readonly List<(string assembly, string[] catalogs)> ContestedAssemblies = new();

        public bool HasErrors =>
            DuplicateIds.Count > 0 || Unassigned.Count > 0 || DefaultCatalogs.Count > 0 || ContestedAssemblies.Count > 0;

        public bool IsEmpty =>
            !HasErrors && Added.Count == 0 && Removed.Count == 0 && MissingUxml.Count == 0;

        public void Log()
        {
            if (Added.Count > 0 || Removed.Count > 0) {
                var changes = new StringBuilder("UIKit: screen catalogs synced.");
                foreach (var (catalog, screenId) in Added) {
                    changes.Append($"\n  + {screenId} ({catalog})");
                }
                foreach (var (catalog, screenId) in Removed) {
                    changes.Append($"\n  - {screenId} ({catalog})");
                }
                Debug.Log(changes.ToString());
            }

            if (MissingUxml.Count > 0) {
                Debug.LogWarning(
                    "UIKit: no UXML for " +
                    string.Join(", ", MissingUxml.Select(m => $"{m.screenId} ({m.catalog})")) +
                    ". Name the file '<screen id>.uxml' or assign it in Project Settings > UI Kit.");
            }

            if (HasErrors) {
                Debug.LogError("UIKit: screen catalog problems:\n  " + string.Join("\n  ", DescribeErrors()));
            }
        }

        public IEnumerable<string> DescribeErrors()
        {
            foreach (var (screenId, types) in DuplicateIds) {
                yield return $"Screen id '{screenId}' is used by {string.Join(", ", types)}. " +
                             "Give one of their ViewModels a distinct [UIScreen] id.";
            }
            foreach (var (screenId, assembly) in Unassigned) {
                yield return $"Screen '{screenId}' ({assembly}) has no catalog: no catalog lists the assembly " +
                             "and there is no default catalog (one with an empty assembly list).";
            }
            if (DefaultCatalogs.Count > 0) {
                yield return $"Several default catalogs ({string.Join(", ", DefaultCatalogs)}); " +
                             $"unclaimed screens go to '{DefaultCatalogs[0]}'. Leave only one assembly list empty.";
            }
            foreach (var (assembly, catalogs) in ContestedAssemblies) {
                yield return $"Assembly '{assembly}' is listed by {string.Join(", ", catalogs)}; " +
                             $"its screens go to '{catalogs[0]}'. List it in one catalog only.";
            }
        }
    }
}
