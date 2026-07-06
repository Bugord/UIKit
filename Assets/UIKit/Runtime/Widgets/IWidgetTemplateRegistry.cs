using UnityEngine.UIElements;

namespace UIKit.Runtime.Widgets
{
    public interface IWidgetTemplateRegistry
    {
        VisualTreeAsset Get(string widgetId);
    }
}
