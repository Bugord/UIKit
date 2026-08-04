using System.Linq;
using UIKit.Runtime.Core;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace UIKit.Editor
{
    // Project Settings > UI Kit tells the user an unresolved UXML blocks the build.
    // This is what makes that true - without it the failure surfaces at runtime, as an
    // exception the first time the screen is opened.
    internal sealed class ScreenCatalogBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var catalog = CatalogLocator.FindSingle<ScreenCatalog>();

            if (catalog == null) {
                return;
            }

            var missing = catalog.Entries
                .Where(entry => entry.uxml == null)
                .Select(entry => entry.screenId)
                .ToArray();

            if (missing.Length == 0) {
                return;
            }

            throw new BuildFailedException(
                $"{nameof(ScreenCatalog)}: no UXML assigned for {string.Join(", ", missing)}. " +
                "Assign it in Project Settings > UI Kit - these screens would throw when opened.");
        }
    }
}
