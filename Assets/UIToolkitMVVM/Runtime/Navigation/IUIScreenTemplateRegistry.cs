using UnityEngine.UIElements;

namespace UIToolkitMVVM.Navigation
{
    public interface IUIScreenTemplateRegistry
    {
        VisualTreeAsset Get(string screenId);
    }
}