using UnityEditor;
using UnityEditor.Callbacks;

namespace UIKit.Editor
{
    internal static class AutoRegistrySync
    {
        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            // During a reload the asset database may still be importing and return no catalogs.
            EditorApplication.delayCall += Run;
        }

        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
                EditorApplication.isUpdating) {
                return;
            }

            var catalogs = CatalogLocator.FindAll();
            if (catalogs.Count == 0) {
                return;
            }

            UIRegistrySync.Sync(catalogs).Log();
        }
    }
}
