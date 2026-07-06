using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UIKit.Editor
{
    public static class UIKitSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider Create()
        {
            Debug.Log("UIKit settings provider registered");
            return new SettingsProvider("Project/UI Kit", SettingsScope.Project) {
                label = "UIKit",
                keywords = new HashSet<string> { "ui", "screen", "popup", "uxml", "catalog" },
                guiHandler = _ => UIKitSettingsGUI.Draw()
            };
        }
    }
}