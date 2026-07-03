namespace UI.Widgets
{
    public interface IWidgetsFactory
    {
        TWidget Create<TWidget>() where TWidget : IWidget;
    }
}