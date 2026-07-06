using UIKit.Runtime.Core;

namespace UIKit.Runtime.Widgets
{
    public interface IWidgetFactory
    {
        TWidget Create<TWidget>() where TWidget : IWidget;
    }
}