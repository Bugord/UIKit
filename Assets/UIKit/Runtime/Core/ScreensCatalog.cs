using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIKit.Runtime.Core
{
    [CreateAssetMenu(menuName = "UIKit/Runtime/ScreensMap")]
    public class ScreensCatalog : ScriptableObject, IScreensCatalog
    {
        [SerializeField]
        private List<VisualTreeAsset> screens;

        public VisualTreeAsset GetAssetFor<TView>(TView view) where TView : IView
        {
            var viewType = view.GetType();
            var viewId = Strip(viewType.Name, "View");

            return screens.Find(screen => screen.name == viewId);
        }

        private static string Strip(string name, string suffix)
        {
            if (name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal)) {
                return name[..^suffix.Length];
            }

            return name;
        }
    }
}