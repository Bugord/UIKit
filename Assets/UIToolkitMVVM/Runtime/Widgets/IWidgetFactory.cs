namespace UIToolkitMVVM.Widgets
{
    public interface IWidgetFactory
    {
        TWidget Create<TWidget>() where TWidget : IWidget;
    }
}