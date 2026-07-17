using UIKit.Runtime.Abstractions;
using UnityEngine.UIElements;

namespace UIKit.Runtime.Core
{
    public class ViewFactory : IViewFactory
    {
        private readonly IScreenResolver screenResolver;
        private readonly ScreenCatalog screenCatalog;

        public ViewFactory(IScreenResolver screenResolver, ScreenCatalog screenCatalog)
        {
            this.screenResolver = screenResolver;
            this.screenCatalog = screenCatalog;
        }

        public TView Create<TView, TViewModel>() where TView : View<TViewModel> where TViewModel : ViewModel
        {
            var view = screenResolver.ResolveView<TView, TViewModel>();
            view.SetViewFactory(this);
            return view;
        }

        public TView Create<TView, TViewModel>(TViewModel viewModel, VisualElement container)
            where TView : View<TViewModel> where TViewModel : ViewModel
        {
            var view = Create<TView, TViewModel>();
            view.Bind(viewModel);

            var id = UIId.OfScreen(typeof(TViewModel));
            screenCatalog.Get(id).uxml.CloneTree(container, out var firstElementIndex, out _);
            var root = container.hierarchy.ElementAt(firstElementIndex);

            view.Attach(root);

            return view;
        }
    }
}
