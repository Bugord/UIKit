using UIKit.Runtime.Abstractions;

namespace UIKit.Runtime.Core
{
    public interface IScreenResolver
    {
        TViewModel Resolve<TViewModel>() where TViewModel : ViewModel;
        TView ResolveView<TView, TViewModel>() where TView : View<TViewModel> where TViewModel : ViewModel;
    }
}