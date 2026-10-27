using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIKit.Editor
{
    internal static class UIKitSettingsGUI
    {
        private static readonly List<SerializedObject> catalogs = new();
        private static Vector2 scroll;
        private static bool stale = true;
        private static GUIStyle errorLabel;

        private static GUIStyle ErrorLabel => errorLabel ??= new GUIStyle(EditorStyles.label) {
            normal = { textColor = new Color(0.95f, 0.35f, 0.3f) }
        };

        static UIKitSettingsGUI()
        {
            // Catalogs created or deleted in the Project window show up without reopening the page.
            EditorApplication.projectChanged += () => stale = true;
        }

        public static void Refresh()
        {
            stale = false;
            catalogs.Clear();
            catalogs.AddRange(CatalogLocator.FindAll().Select(catalog => new SerializedObject(catalog)));
        }

        public static void Draw()
        {
            if (stale || catalogs.Any(serialized => serialized.targetObject == null)) {
                Refresh();
            }

            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Sync with code", GUILayout.Width(140))) {
                    Sync();
                }

                if (catalogs.Count == 0 && GUILayout.Button("Create catalog", GUILayout.Width(140))) {
                    CreateCatalog();
                }

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(8);

            if (catalogs.Count == 0) {
                EditorGUILayout.HelpBox(
                    $"No {nameof(ScreenCatalog)} asset found. Create one to map screens to their UXML.",
                    MessageType.Warning);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var serialized in catalogs) {
                DrawCatalog(serialized);
                EditorGUILayout.Space(12);
            }
            EditorGUILayout.EndScrollView();
        }

        private static void DrawCatalog(SerializedObject serialized)
        {
            serialized.Update();

            // Shown to ping the asset; the field's result is ignored.
            EditorGUILayout.ObjectField(serialized.targetObject, typeof(ScreenCatalog), false);

            // SerializedProperty edits go through Undo on their own.
            EditorGUILayout.PropertyField(serialized.FindProperty("assemblies"));

            var entries = serialized.FindProperty("entries");
            var missing = 0;

            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField("Screen", EditorStyles.miniBoldLabel, GUILayout.Width(220));
                EditorGUILayout.LabelField("UXML", EditorStyles.miniBoldLabel);
            }

            for (var i = 0; i < entries.arraySize; i++) {
                var entry = entries.GetArrayElementAtIndex(i);
                var screenId = entry.FindPropertyRelative("screenId").stringValue;
                var uxml = entry.FindPropertyRelative("uxml");
                var hasUxml = uxml.objectReferenceValue != null;
                if (!hasUxml) {
                    missing++;
                }

                using (new EditorGUILayout.HorizontalScope()) {
                    EditorGUILayout.LabelField(hasUxml ? screenId : $"! {screenId}",
                        hasUxml ? EditorStyles.label : ErrorLabel, GUILayout.Width(220));
                    EditorGUILayout.ObjectField(uxml, typeof(VisualTreeAsset), GUIContent.none);
                }
            }

            serialized.ApplyModifiedProperties();

            if (missing > 0) {
                EditorGUILayout.HelpBox(
                    $"{missing} screen(s) without UXML. Name the file after the screen id and sync, " +
                    "or assign it here. The build fails until this is resolved.",
                    MessageType.Error);
            }
        }

        private static void Sync()
        {
            var report = UIRegistrySync.Sync(catalogs.Select(serialized => (ScreenCatalog)serialized.targetObject).ToList());
            if (report.IsEmpty) {
                Debug.Log("UIKit: screen catalogs are already in sync.");
            } else {
                report.Log();
            }
        }

        private static void CreateCatalog()
        {
            var path = AssetDatabase.GenerateUniqueAssetPath($"Assets/{nameof(ScreenCatalog)}.asset");
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ScreenCatalog>(), path);
            AssetDatabase.SaveAssets();

            Refresh();
            Sync();
        }
    }
}
