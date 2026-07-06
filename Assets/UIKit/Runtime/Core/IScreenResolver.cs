using UIKit.Runtime.Abstractions;

namespace UIKit.Runtime.Core
{
    public interface IScreenResolver
    {
        TViewModel Resolve<TViewModel>() where TViewModel : ViewModel;
        View<TViewModel> ResolveView<TViewModel>() where TViewModel : ViewModel;
    }
}