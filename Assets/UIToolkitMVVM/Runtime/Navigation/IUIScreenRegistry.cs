using UnityEngine.UIElements;

namespace UIToolkitMVVM.Navigation
{
    public interface IUIScreenRegistry
    {
        VisualTreeAsset Get(string screenId);
    }
}