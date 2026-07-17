using UIKit.Runtime.Abstractions;
using UIKit.Runtime.Utils;
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
            var root = new VisualElement { name = $"{id}Root" };
            root.StretchToParent();
            screenCatalog.Get(id).uxml.CloneTree(root);

            view.Attach(root);
            container.Add(view.Root);

            return view;
        }
    }
}
