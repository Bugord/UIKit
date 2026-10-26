using System;
using System.Threading;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIKit
{
    /// <summary>
    /// Presents a <typeparamref name="TViewModel"/>. The view's root comes from the UXML its
    /// catalog lists, or from <see cref="CreateRoot"/> for a view built in code.
    /// </summary>
    /// <remarks>
    /// The root's <c>dataSource</c> is the ViewModel, so UXML data bindings resolve against it.
    /// For changes a binding cannot express, override <see cref="OnPropertyChanged"/>.
    /// </remarks>
    public abstract class View<TViewModel> : IView, IViewBinder where TViewModel : ViewModel
    {
        private EventHandler<BindablePropertyChangedEventArgs> propertyChangedHandler;
        private ChildViewFactory childViews;
        private bool disposeViewModel = true;
        private Action<IView> disposed;

        public VisualElement Root { get; private set; }
        public TViewModel ViewModel { get; private set; }
        public bool IsDisposed { get; private set; }

        ViewModel IView.ViewModel => ViewModel;

        /// <summary>
        /// Creates child views. They are disposed together with this view, or earlier if
        /// disposed directly.
        /// </summary>
        protected IViewFactory ViewFactory => childViews;

        event Action<IView> IViewBinder.Disposed
        {
            add => disposed += value;
            remove => disposed -= value;
        }

        public virtual Awaitable ShowAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Root.style.display = DisplayStyle.Flex;
            return Awaitables.Completed();
        }

        public virtual Awaitable HideAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Root.style.display = DisplayStyle.None;
            return Awaitables.Completed();
        }

        /// <summary>
        /// Disposes the child views, runs <see cref="OnDispose"/>, removes the root from the
        /// hierarchy and disposes the ViewModel unless it was created with
        /// <c>disposeViewModel: false</c>. Safe to call more than once.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed) {
                return;
            }

            IsDisposed = true;

            try {
                childViews?.DisposeAll();
                OnDispose();
            } finally {
                if (ViewModel != null && propertyChangedHandler != null) {
                    ViewModel.propertyChanged -= propertyChangedHandler;
                }

                Root?.RemoveFromHierarchy();
                Root = null;

                try {
                    if (disposeViewModel) {
                        ViewModel?.Dispose();
                    }
                } finally {
                    var handler = disposed;
                    disposed = null;
                    handler?.Invoke(this);
                }
            }
        }

        /// <summary>
        /// Builds the root element in code. Return <c>null</c> (the default) to clone the UXML
        /// the catalog lists for this screen instead.
        /// </summary>
        protected virtual VisualElement CreateRoot() => null;

        /// <summary>
        /// Called once the root exists and the ViewModel is bound: query elements and hook up
        /// events here.
        /// </summary>
        protected virtual void OnAttach()
        {
        }

        /// <summary>
        /// Called when the ViewModel raises a property change.
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
        }

        protected virtual void OnDispose()
        {
        }

        /// <summary>
        /// Binds <paramref name="elementProperty"/> of <paramref name="element"/> to
        /// <paramref name="viewModelProperty"/>, the code equivalent of a UXML data binding.
        /// The ViewModel property needs <c>[CreateProperty]</c>.
        /// </summary>
        protected DataBinding BindProperty(VisualElement element, string elementProperty, string viewModelProperty,
            BindingMode mode = BindingMode.ToTarget)
        {
            var binding = new DataBinding {
                dataSourcePath = new PropertyPath(viewModelProperty),
                bindingMode = mode
            };

            element.SetBinding(elementProperty, binding);
            return binding;
        }

        void IViewBinder.Bind(ViewModel viewModel, bool disposeOwnedViewModel, ViewFactory viewFactory)
        {
            if (viewModel is not TViewModel typed) {
                throw new ArgumentException(
                    $"{GetType().Name} presents a {typeof(TViewModel).Name}, " +
                    $"but was given a {viewModel?.GetType().Name ?? "null"}.", nameof(viewModel));
            }

            ViewModel = typed;
            disposeViewModel = disposeOwnedViewModel;
            childViews = new ChildViewFactory(viewFactory);
        }

        VisualElement IViewBinder.CreateRoot() => CreateRoot();

        void IViewBinder.Attach(VisualElement root)
        {
            Root = root;
            Root.dataSource = ViewModel;

            propertyChangedHandler = (_, args) => OnPropertyChanged(args.propertyName.ToString());
            ViewModel.propertyChanged += propertyChangedHandler;

            OnAttach();
        }
    }
}
