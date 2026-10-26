using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIKit
{
    // The factory a view hands its subclass: tracks the child views it creates so they are
    // disposed with the parent.
    internal sealed class ChildViewFactory : IViewFactory
    {
        private readonly ViewFactory viewFactory;
        private readonly List<IView> children = new();
        private readonly Action<IView> untrack;

        private bool disposed;

        public ChildViewFactory(ViewFactory viewFactory)
        {
            this.viewFactory = viewFactory;
            untrack = child => children.Remove(child);
        }

        public int Count => children.Count;

        public TView Create<TView, TViewModel>(TViewModel viewModel, VisualElement container,
            bool disposeViewModel = true)
            where TView : View<TViewModel> where TViewModel : ViewModel
        {
            ThrowIfDisposed();
            return Track(viewFactory.Create<TView, TViewModel>(viewModel, container, disposeViewModel));
        }

        public IView Create(ViewModel viewModel, VisualElement container, bool disposeViewModel = true)
        {
            ThrowIfDisposed();
            return Track(viewFactory.Create(viewModel, container, disposeViewModel));
        }

        public void DisposeAll()
        {
            disposed = true;

            // Copied first: each child removes itself from the list as it is disposed.
            var toDispose = children.ToArray();
            children.Clear();

            for (var i = toDispose.Length - 1; i >= 0; i--) {
                try {
                    toDispose[i].Dispose();
                } catch (Exception exception) {
                    Debug.LogException(exception);
                }
            }
        }

        private TView Track<TView>(TView view) where TView : IView
        {
            children.Add(view);
            ((IViewBinder)view).Disposed += untrack;
            return view;
        }

        private void ThrowIfDisposed()
        {
            if (disposed) {
                throw new ObjectDisposedException(nameof(IViewFactory),
                    "The parent view has been disposed, so it can no longer create child views.");
            }
        }
    }
}
