using UnityEngine.UIElements;

namespace UIKit
{
    /// <summary>
    /// Supplies the element each <see cref="UILayer"/>'s views are added to. The popup layer
    /// must render above the screen layer.
    /// </summary>
    public interface IUIRootProvider
    {
        VisualElement GetLayerRoot(UILayer layer);
    }
}
