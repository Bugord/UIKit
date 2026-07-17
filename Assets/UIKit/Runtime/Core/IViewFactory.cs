using UIKit.Runtime.Abstractions;
using UnityEngine.UIElements;

namespace UIKit.Runtime.Core
{
    public interface IViewFactory
    {
        TView Create<TView, TViewModel>() where TView : View<TViewModel> where TViewModel : ViewModel;

        TView Create<TView, TViewModel>(TViewModel viewModel, VisualElement container)
            where TView : View<TViewModel> where TViewModel : ViewModel;
    }
}
