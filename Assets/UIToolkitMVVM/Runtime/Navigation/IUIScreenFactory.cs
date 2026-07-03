namespace UI.Navigation
{
    public interface IUIScreenFactory
    {
        TController Create<TController>() where TController : IView;
    }
}
