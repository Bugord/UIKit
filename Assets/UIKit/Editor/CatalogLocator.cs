using UnityEditor;
using UnityEngine;

namespace UIKit.Editor
{
    internal static class CatalogLocator
    {
        public static T FindSingle<T>() where T : ScriptableObject
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length == 0) {
                return null;
            }

            if (guids.Length > 1) {
                Debug.LogWarning($"Multiple {typeof(T).Name} assets found, using the first.");
            }

            return AssetDatabase.LoadAssetAtPath<T>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
