using System;
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
            var id = UIId.OfScreen(typeof(TViewModel));
            var uxml = screenCatalog.Get(id).uxml;

            if (uxml == null) {
                throw new InvalidOperationException(
                    $"{nameof(ScreenCatalog)}: no UXML assigned for screen '{id}'. " +
                    $"Name the asset '{id}.uxml' and press \"Sync with code\", or assign it " +
                    "manually in Project Settings > UI Kit.");
            }

            var view = Create<TView, TViewModel>();
            view.Bind(viewModel);

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

            return view;
        }

        private static void RemoveClonedElements(VisualElement container, int firstElementIndex, int count)
        {
            for (var i = firstElementIndex + count - 1; i >= firstElementIndex; i--) {
                container.hierarchy.RemoveAt(i);
            }
        }
    }
}
