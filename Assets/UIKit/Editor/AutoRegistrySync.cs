using UIKit.Runtime.Core;
using UnityEditor.Callbacks;

namespace UIKit.Editor
{
    internal static class AutoRegistrySync
    {
        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            var screenCatalog = CatalogLocator.FindSingle<ScreenCatalog>();
            if (screenCatalog != null) {
                UIRegistrySync.Sync(screenCatalog);
            }

            var widgetCatalog = CatalogLocator.FindSingle<WidgetCatalog>();
            if (widgetCatalog != null) {
                WidgetRegistrySync.Sync(widgetCatalog);
            }
        }
    }
}
