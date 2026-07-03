using UnityEngine.UIElements;

namespace UI.Widgets
{
    public interface IWidgetTemplateRegistry
    {
        VisualTreeAsset Get(string widgetId);
    }
}
