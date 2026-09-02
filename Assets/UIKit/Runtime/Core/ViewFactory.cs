using System;
using UIKit.Runtime.Abstractions;
using UnityEngine.UIElements;

namespace UIKit.Runtime.Core
{
    public class ViewFactory : IViewFactory
    {
        private readonly ScreenRegistry screenRegistry;
        private readonly ScreenCatalog screenCatalog;

        public ViewFactory(ScreenRegistry screenRegistry, ScreenCatalog screenCatalog)
        {
            this.screenRegistry = screenRegistry;
            this.screenCatalog = screenCatalog;
        }

        public TView Create<TView, TViewModel>() where TView : View<TViewModel> where TViewModel : ViewModel
        {
            // The cast holds because the binding was registered with this ViewModel's own
            // View type: TView is either that type or a base of it.
            var view = (TView)screenRegistry.Get(typeof(TViewModel)).CreateView();
            view.SetViewFactory(this);
            return view;
        }

        public TView Create<TView, TViewModel>(TViewModel viewModel, VisualElement container,
            bool disposeViewModel = true)
            where TView : View<TViewModel> where TViewModel : ViewModel
        {
            var id = UIId.OfScreen(typeof(TViewModel));
            var uxml = RequireUxml(id);

            var view = Create<TView, TViewModel>();
            ((IViewBinder)view).BindModel(viewModel, disposeViewModel);

            CloneAndAttach(view, container, id, uxml);

            return view;
        }

        public IView Create(ViewModel viewModel, VisualElement container, bool disposeViewModel = true)
        {
            if (viewModel == null) {
                throw new ArgumentNullException(nameof(viewModel));
            }

            // Resolved by concrete runtime type - the key each RegisterScreen call uses.
            var viewModelType = viewModel.GetType();
            var id = UIId.OfScreen(viewModelType);
            var uxml = RequireUxml(id);

            var view = screenRegistry.Get(viewModelType).CreateView();
            var binder = (IViewBinder)view;
            binder.SetViewFactory(this);
            binder.BindModel(viewModel, disposeViewModel);

            CloneAndAttach(view, container, id, uxml);

            return view;
        }

        private VisualTreeAsset RequireUxml(string id)
        {
            var uxml = screenCatalog.Get(id).uxml;

            if (uxml == null) {
                throw new InvalidOperationException(
                    $"{nameof(ScreenCatalog)}: no UXML assigned for screen '{id}'. " +
                    $"Name the asset '{id}.uxml' and press \"Sync with code\", or assign it " +
                    "manually in Project Settings > UI Kit.");
            }

            return uxml;
        }

        private static void CloneAndAttach(IView view, VisualElement container, string id, VisualTreeAsset uxml)
        {
            uxml.CloneTree(container, out var firstElementIndex, out var elementAddedCount);

            if (elementAddedCount != 1) {
                // A view exposes a single Root and only detaches that one on Dispose, so
                // sibling roots would be orphaned in the layer container forever.
                RemoveClonedElements(container, firstElementIndex, elementAddedCount);
                view.Dispose();

                throw new InvalidOperationException(
                    $"{nameof(ScreenCatalog)}: UXML for screen '{id}' produced {elementAddedCount} " +
                    "root elements, but a view requires exactly one. Wrap its contents in a " +
                    "single root element.");
            }

            view.Attach(container.hierarchy.ElementAt(firstElementIndex));
        }

        private static void RemoveClonedElements(VisualElement container, int firstElementIndex, int count)
        {
            for (var i = firstElementIndex + count - 1; i >= firstElementIndex; i--) {
                container.hierarchy.RemoveAt(i);
            }
        }
    }
}
