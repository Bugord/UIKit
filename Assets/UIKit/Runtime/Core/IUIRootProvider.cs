using UnityEngine.UIElements;

namespace UIKit.Runtime.Core
{
    public interface IUIRootProvider
    {
        VisualElement GetLayerRoot(UILayer layer);
    }
}