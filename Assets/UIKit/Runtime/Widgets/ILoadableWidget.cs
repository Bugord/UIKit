using UIKit.Runtime.Core;

namespace UIKit.Runtime.Widgets
{
    public interface ILoadableWidget : IWidget
    {
        string WidgetId { get; }
    }
}
