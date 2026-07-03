namespace UIToolkitMVVM.Widgets
{
    public interface IWidgetsFactory
    {
        TWidget Create<TWidget>() where TWidget : IWidget;
    }
}