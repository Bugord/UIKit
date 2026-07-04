using UIToolkitMVVM.Views;

namespace UIToolkitMVVM.Navigation
{
    public interface IUIViewFactory
    {
        TController Create<TController>() where TController : IView;
    }
}
