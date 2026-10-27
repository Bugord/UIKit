using UnityEngine.UIElements;

namespace UIKit
{
    /// <summary>
    /// Creates views for ViewModels, e.g. a child view inside a screen. A view's own
    /// <see cref="View{TViewModel}.ViewFactory"/> disposes the views it created together with
    /// that view.
    /// </summary>
    public interface IViewFactory
    {
        /// <summary>
        /// Creates the view registered for <typeparamref name="TViewModel"/>, adds it to
        /// <paramref name="container"/> and binds it to <paramref name="viewModel"/>. Pass
        /// <paramref name="disposeViewModel"/> as <c>false</c> when something else owns the
        /// ViewModel's lifetime.
        /// </summary>
        TView Create<TView, TViewModel>(TViewModel viewModel, VisualElement container,
            bool disposeViewModel = true)
            where TView : View<TViewModel> where TViewModel : ViewModel;

        /// <summary>
        /// Resolves the view by <paramref name="viewModel"/>'s runtime type, for when the
        /// compile-time type is only <see cref="ViewModel"/>.
        /// </summary>
        IView Create(ViewModel viewModel, VisualElement container, bool disposeViewModel = true);
    }
}
