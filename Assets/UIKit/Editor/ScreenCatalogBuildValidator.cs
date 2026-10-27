using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace UIKit.Editor
{
    // Without this, a catalog problem surfaces at runtime, as an exception the first time the
    // screen is opened.
    internal sealed class ScreenCatalogBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var catalogs = CatalogLocator.FindAll().Select(CatalogState.Of).ToList();
            var problems = Validate(catalogs, ScreenDiscovery.FindScreens());

            if (problems.Count == 0) {
                return;
            }

            throw new BuildFailedException(
                $"{nameof(ScreenCatalog)}: screens would fail to open at runtime. Press " +
                "\"Sync with code\" in Project Settings > UI Kit, then fix what remains:\n  " + string.Join("\n  ", problems));
        }

        public static List<string> Validate(IReadOnlyList<CatalogState> catalogs, IReadOnlyList<ScreenSource> screens)
        {
            var problems = new List<string>();
            var listedIn = new Dictionary<string, List<string>>();

            foreach (var catalog in catalogs) {
                foreach (var entry in catalog.Entries) {
                    if (entry == null || string.IsNullOrEmpty(entry.screenId)) {
                        continue;
                    }

                    if (!listedIn.TryGetValue(entry.screenId, out var names)) {
                        listedIn[entry.screenId] = names = new List<string>();
                    }
                    names.Add(catalog.Name);

                    if (entry.uxml == null) {
                        problems.Add($"'{entry.screenId}' in '{catalog.Name}' has no UXML.");
                    }
                }
            }

            foreach (var (screenId, names) in listedIn.OrderBy(pair => pair.Key, StringComparer.Ordinal)) {
                if (names.Count > 1) {
                    problems.Add($"'{screenId}' is listed in {string.Join(", ", names)}; a screen belongs to one catalog.");
                }
            }

            var byId = screens
                .Where(screen => !string.IsNullOrEmpty(screen.ScreenId))
                .GroupBy(screen => screen.ScreenId)
                .OrderBy(group => group.Key, StringComparer.Ordinal);

            foreach (var group in byId) {
                if (group.Count() > 1) {
                    var types = group.Select(screen => screen.TypeName).OrderBy(name => name, StringComparer.Ordinal);
                    problems.Add($"'{group.Key}' is the id of {string.Join(", ", types)}.");
                }

                if (!listedIn.ContainsKey(group.Key)) {
                    problems.Add($"'{group.Key}' is in no catalog.");
                }
            }

            return problems;
        }
    }
}
