using UnityEngine.UIElements;

namespace UI.Navigation
{
    public interface IUIScreenRegistry
    {
        VisualTreeAsset Get(string screenId);
    }
}