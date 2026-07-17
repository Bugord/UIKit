using System.Linq;
using UIKit.Runtime.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIKit.Editor
{
    internal static class UIKitSettingsGUI
    {
        private static Vector2 scroll;
 
        public static void Draw()
        {
            var screenCatalog = CatalogLocator.FindSingle<ScreenCatalog>();

            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Sync with code", GUILayout.Width(140))) {
                    if (screenCatalog != null) {
                        UIRegistrySync.Sync(screenCatalog);
                    }
                }

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(8);
            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawScreenCatalog(screenCatalog);

            EditorGUILayout.EndScrollView();
        }
 
        private static void DrawScreenCatalog(ScreenCatalog catalog)
        {
            EditorGUILayout.LabelField("Screens", EditorStyles.boldLabel);
 
            if (catalog == null) {
                DrawMissingCatalog<ScreenCatalog>("Screen Catalog");
                return;
            }
 
            EditorGUILayout.ObjectField(catalog, typeof(ScreenCatalog), false);
            EditorGUILayout.Space(4);
 
            DrawHeader();
 
            var dirty = false;
 
            foreach (var entry in catalog.Entries) {
                using (new EditorGUILayout.HorizontalScope()) {
                    var missing = entry.uxml == null;
 
                    EditorGUILayout.LabelField(
                        missing ? $"! {entry.screenId}" : entry.screenId,
                        missing ? ErrorLabel : EditorStyles.label,
                        GUILayout.Width(220));
 
                    EditorGUILayout.LabelField(entry.layer.ToString(), GUILayout.Width(70));
 
                    EditorGUI.BeginChangeCheck();
                    var uxml = (VisualTreeAsset)EditorGUILayout.ObjectField(
                        entry.uxml, typeof(VisualTreeAsset), false);
                    if (EditorGUI.EndChangeCheck()) {
                        entry.uxml = uxml;
                        dirty = true;
                    }
                }
            }
 
            if (dirty) {
                EditorUtility.SetDirty(catalog);
            }
 
            var missingCount = catalog.Entries.Count(e => e.uxml == null);
            if (missingCount > 0) {
                EditorGUILayout.HelpBox(
                    $"{missingCount} screen(s) without UXML. Name the file after the screen id " +
                    "or assign it manually. The build will fail until resolved.",
                    MessageType.Error);
            }
        }
 
        private static void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField("Screen", EditorStyles.miniBoldLabel, GUILayout.Width(220));
                EditorGUILayout.LabelField("Layer", EditorStyles.miniBoldLabel, GUILayout.Width(70));
                EditorGUILayout.LabelField("UXML", EditorStyles.miniBoldLabel);
            }
        }
 
        private static void DrawMissingCatalog<T>(string menuName) where T : ScriptableObject
        {
            EditorGUILayout.HelpBox(
                $"No {typeof(T).Name} asset found. Create one via Assets/Create/UIKit/{menuName}.",
                MessageType.Warning);
        }
 
        private static GUIStyle errorLabel;
        private static GUIStyle ErrorLabel => errorLabel ??= new GUIStyle(EditorStyles.label) {
            normal = { textColor = new Color(0.95f, 0.35f, 0.3f) }
        };
    }
}