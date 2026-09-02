using UIKit.Runtime.Abstractions;
using UnityEngine.UIElements;

namespace UIKit.Runtime.Core
{
    public interface IViewFactory
    {
        TView Create<TView, TViewModel>() where TView : View<TViewModel> where TViewModel : ViewModel;

        /// <summary>
        /// Pass <paramref name="disposeViewModel"/> as <c>false</c> to stop the returned view
        /// from disposing <paramref name="viewModel"/> with it, e.g. when a parent owns that
        /// ViewModel's lifetime.
        /// </summary>
        TView Create<TView, TViewModel>(TViewModel viewModel, VisualElement container,
            bool disposeViewModel = true)
            where TView : View<TViewModel> where TViewModel : ViewModel;

        /// <summary>
        /// Resolves the view by <paramref name="viewModel"/>'s concrete runtime type, for when
        /// the compile-time type is only the shared <see cref="ViewModel"/> base. Prefer the
        /// generic overloads when the type is known. <paramref name="disposeViewModel"/> works
        /// as on the generic overload.
        /// </summary>
        IView Create(ViewModel viewModel, VisualElement container, bool disposeViewModel = true);
    }
}
