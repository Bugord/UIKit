using UnityEngine.UIElements;

namespace UIToolkitMVVM.Widgets
{
    public interface IWidgetTemplateRegistry
    {
        VisualTreeAsset Get(string widgetId);
    }
}
