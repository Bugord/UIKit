using UIToolkitMVVM.Views;

namespace UIToolkitMVVM.Navigation
{
    public interface IUIScreenFactory
    {
        TController Create<TController>() where TController : IView;
    }
}
