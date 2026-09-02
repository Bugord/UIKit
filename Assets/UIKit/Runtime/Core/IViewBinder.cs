using UIKit.Runtime.Abstractions;

namespace UIKit.Runtime.Core
{
    /// <summary>
    /// Non-generic wiring hooks for a resolved view, used by the type-erased
    /// <c>IViewFactory.Create(ViewModel, VisualElement, bool)</c> overload. Implemented
    /// explicitly by <see cref="View{TViewModel}"/> to stay off the public surface.
    /// </summary>
    internal interface IViewBinder
    {
        void SetViewFactory(IViewFactory viewFactory);

        void BindModel(ViewModel viewModel, bool disposeViewModel);
    }
}
