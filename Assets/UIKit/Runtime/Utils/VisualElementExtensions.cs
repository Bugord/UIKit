using UnityEngine.UIElements;

namespace UIKit.Runtime.Utils
{
    internal static class VisualElementExtensions
    {
        public static void StretchToParent(this VisualElement element)
        {
            element.style.position = Position.Absolute;
            element.style.left = 0;
            element.style.top = 0;
            element.style.right = 0;
            element.style.bottom = 0;
            element.pickingMode = PickingMode.Ignore;
        }
    }
}
