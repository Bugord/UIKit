using System;
using UnityEngine.UIElements;

namespace UIKit
{
    internal sealed class ViewFactory : IViewFactory
    {
        private readonly ScreenRegistry screenRegistry;
        private readonly CatalogSet catalogs;

        public ViewFactory(ScreenRegistry screenRegistry, CatalogSet catalogs)
        {
            this.screenRegistry = screenRegistry;
            this.catalogs = catalogs;
        }

        public TView Create<TView, TViewModel>(TViewModel viewModel, VisualElement container,
            bool disposeViewModel = true)
            where TView : View<TViewModel> where TViewModel : ViewModel
        {
            var view = CreateView(typeof(TViewModel), viewModel, container, disposeViewModel);

            if (view is TView typed) {
                return typed;
            }

            view.Dispose();

            throw new InvalidOperationException(
                $"Screen '{typeof(TViewModel).Name}' is registered with view '{view.GetType().Name}', " +
                $"which is not a {typeof(TView).Name}.");
        }

        public IView Create(ViewModel viewModel, VisualElement container, bool disposeViewModel = true)
        {
            if (viewModel == null) {
                throw new ArgumentNullException(nameof(viewModel));
            }

            // Resolved by concrete runtime type - the key each Register call uses.
            return CreateView(viewModel.GetType(), viewModel, container, disposeViewModel);
        }

        /// <summary>
        /// Creates, binds and attaches the view registered under <paramref name="viewModelType"/>.
        /// If this throws after the view took <paramref name="viewModel"/>, the view is disposed,
        /// and with it the ViewModel when <paramref name="disposeViewModel"/> is set.
        /// </summary>
        public IView CreateView(Type viewModelType, ViewModel viewModel, VisualElement container,
            bool disposeViewModel)
        {
            if (viewModel == null) {
                throw new ArgumentNullException(nameof(viewModel));
            }

            if (container == null) {
                throw new ArgumentNullException(nameof(container));
            }

            var view = screenRegistry.Get(viewModelType).CreateView();

            if (view is not IViewBinder binder) {
                throw new InvalidOperationException(
                    $"View '{view?.GetType().Name ?? "null"}' for screen '{viewModelType.Name}' must derive " +
                    "from View<TViewModel>.");
            }

            try {
                binder.Bind(viewModel, disposeViewModel, this);

                var root = binder.CreateRoot();

                if (root != null) {
                    container.Add(root);
                } else {
                    root = CloneSingleRoot(container, ScreenInfo.Of(viewModelType).Id);
                }

                binder.Attach(root);
                return view;
            } catch {
                view.Dispose();
                throw;
            }
        }

        private VisualElement CloneSingleRoot(VisualElement container, string screenId)
        {
            var uxml = catalogs.GetUxml(screenId);

            uxml.CloneTree(container, out var firstElementIndex, out var elementAddedCount);

            if (elementAddedCount != 1) {
                // A view exposes a single Root and only detaches that one on Dispose, so
                // sibling roots would be orphaned in the container forever.
                for (var i = firstElementIndex + elementAddedCount - 1; i >= firstElementIndex; i--) {
                    container.hierarchy.RemoveAt(i);
                }

                throw new InvalidOperationException(
                    $"{nameof(ScreenCatalog)}: UXML for screen '{screenId}' produced {elementAddedCount} " +
                    "root elements, but a view requires exactly one. Wrap its contents in a single root element.");
            }

            return container.hierarchy.ElementAt(firstElementIndex);
        }
    }
}
