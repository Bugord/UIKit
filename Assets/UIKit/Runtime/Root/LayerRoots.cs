using UnityEngine.UIElements;

namespace UIKit
{
    internal static class LayerRoots
    {
        public const string ScreenRootName = "ScreenRoot";
        public const string PopupRootName = "PopupsRoot";

        public static VisualElement Create(string name)
        {
            var element = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            element.style.position = Position.Absolute;
            element.style.left = 0;
            element.style.top = 0;
            element.style.right = 0;
            element.style.bottom = 0;
            return element;
        }
    }
}
