using System.Collections.Generic;
using UnityEditor;

namespace UIKit.Editor
{
    public static class UIKitSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new SettingsProvider("Project/UI Kit", SettingsScope.Project) {
                label = "UI Kit",
                keywords = new HashSet<string> { "ui", "screen", "popup", "uxml", "catalog" },
                activateHandler = (_, _) => UIKitSettingsGUI.Refresh(),
                guiHandler = _ => UIKitSettingsGUI.Draw()
            };
        }
    }
}
