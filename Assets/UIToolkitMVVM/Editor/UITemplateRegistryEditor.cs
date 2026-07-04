using System.Linq;
using UIToolkitMVVM.Navigation;
using UnityEditor;
using UnityEngine;

namespace UIToolkitMVVM.Editor
{
    [CustomEditor(typeof(UITemplateRegistry))]
    public class UITemplateRegistryEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Sync From Code")) {
                Sync();
            }
        }

        private void Sync()
        {
            var registry = (UITemplateRegistry)target;

            var screens = UITemplateRegistrySync.SyncScreens(registry);
            var widgets = UITemplateRegistrySync.SyncWidgets(registry);

            EditorUtility.SetDirty(registry);

            Debug.Log($"Template registry sync: added {screens.Added} screen(s), {widgets.Added} widget(s).");

            foreach (var warning in screens.Warnings.Concat(widgets.Warnings)) {
                Debug.LogWarning(warning, registry);
            }
        }
    }
}
