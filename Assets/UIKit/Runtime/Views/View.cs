using System;
using System.Collections.Generic;
using System.Linq;
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
    /// <para>
    /// The root's <c>dataSource</c> is the ViewModel, so UXML data bindings resolve against it.
    /// In code, bind in <see cref="OnAttach"/> with <see cref="BindText(string, string)"/>,
    /// <see cref="BindValue(string, string, BindingMode)"/>, <see cref="BindProperty"/> and
    /// <see cref="BindCommand(string, ICommand)"/>. For a reaction a binding cannot express (an
    /// effect, an animation), register it with <see cref="Observe(string, Action)"/>;
    /// <see cref="OnPropertyChanged"/> still sees every change.
    /// </para>
    /// <para>
    /// In the editor and in development builds, the bind and observe methods check the ViewModel
    /// property when they are called and throw <see cref="ArgumentException"/> for a typo or a
    /// property without <c>[CreateProperty]</c>, instead of leaving a binding that silently never
    /// updates. Release builds skip the check.
    /// </para>
    /// </remarks>
    public abstract class View<TViewModel> : IView, IViewBinder where TViewModel : ViewModel
    {
        private EventHandler<BindablePropertyChangedEventArgs> propertyChangedHandler;
        private ChildViewFactory childViews;
        private bool disposeViewModel = true;
        private Action<IView> disposed;
        private Dictionary<string, List<Action>> observers;
        private List<Action> commandUnbinds;

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
        /// Disposes the child views, runs <see cref="OnDispose"/>, unbinds its commands, removes
        /// the root from the hierarchy and disposes the ViewModel unless it was created with
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

                observers = null;
                UnbindCommands();

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
        /// Called once the root exists and the ViewModel is bound: query elements, bind them and
        /// hook up events here.
        /// </summary>
        protected virtual void OnAttach()
        {
        }

        /// <summary>
        /// Called when the ViewModel raises a property change, after the callbacks registered
        /// with <see cref="Observe(string, Action)"/> for that property.
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
        }

        protected virtual void OnDispose()
        {
        }

        /// <summary>
        /// The element named <paramref name="name"/> under <see cref="Root"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">There is no <typeparamref name="T"/> with that name.</exception>
        protected T Require<T>(string name) where T : VisualElement
        {
            if (Root == null) {
                throw new InvalidOperationException(
                    $"{GetType().Name} has no root: Require<{typeof(T).Name}>(\"{name}\") works only once the " +
                    "view is attached and until it is disposed.");
            }

            return Root.Q<T>(name) ?? throw new InvalidOperationException(
                $"{GetType().Name}: no {typeof(T).Name} named '{name}' under the view's root.");
        }

        /// <summary>
        /// Binds <paramref name="elementProperty"/> of <paramref name="element"/> to
        /// <paramref name="viewModelProperty"/>, the code equivalent of a UXML data binding.
        /// The ViewModel property needs <c>[CreateProperty]</c>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="element"/> is null.</exception>
        /// <exception cref="ArgumentException">A name is null or empty or, in the editor or a development build,
        /// the ViewModel has no such property, or <paramref name="mode"/> writes to the ViewModel and the
        /// property is read-only.</exception>
        protected DataBinding BindProperty(VisualElement element, string elementProperty, string viewModelProperty,
            BindingMode mode = BindingMode.ToTarget)
        {
            if (element == null) {
                throw new ArgumentNullException(nameof(element));
            }

            if (string.IsNullOrEmpty(elementProperty)) {
                throw new ArgumentException("An element property is required.", nameof(elementProperty));
            }

            ValidatePath(viewModelProperty, nameof(viewModelProperty),
                mode is BindingMode.TwoWay or BindingMode.ToSource);

            var binding = new DataBinding {
                dataSourcePath = new PropertyPath(viewModelProperty),
                bindingMode = mode
            };

            element.SetBinding(elementProperty, binding);
            return binding;
        }

        /// <summary>
        /// Binds the <c>text</c> of the <see cref="TextElement"/> (a <see cref="Label"/>, a
        /// <see cref="Button"/>...) named <paramref name="elementName"/> to
        /// <paramref name="viewModelProperty"/>, one way.
        /// </summary>
        /// <exception cref="InvalidOperationException">There is no such element.</exception>
        /// <exception cref="ArgumentException">As for <see cref="BindProperty"/>.</exception>
        protected DataBinding BindText(string elementName, string viewModelProperty) =>
            BindText(Require<TextElement>(elementName), viewModelProperty);

        /// <summary>
        /// Binds the <c>text</c> of <paramref name="element"/> to
        /// <paramref name="viewModelProperty"/>, one way.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="element"/> is null.</exception>
        /// <exception cref="ArgumentException">As for <see cref="BindProperty"/>.</exception>
        protected DataBinding BindText(TextElement element, string viewModelProperty) =>
            BindProperty(element, "text", viewModelProperty);

        /// <summary>
        /// Binds the <c>value</c> of the field named <paramref name="elementName"/> (a
        /// <see cref="Slider"/>, a <see cref="Toggle"/>, a <see cref="TextField"/>...) to
        /// <paramref name="viewModelProperty"/>, two-way by default, so the ViewModel's setter
        /// receives what the user enters.
        /// </summary>
        /// <exception cref="InvalidOperationException">There is no such element, or it has no value to bind
        /// (it does not implement <see cref="INotifyValueChanged{T}"/>).</exception>
        /// <exception cref="ArgumentException">As for <see cref="BindProperty"/>.</exception>
        protected DataBinding BindValue(string elementName, string viewModelProperty,
            BindingMode mode = BindingMode.TwoWay)
        {
            var element = Require<VisualElement>(elementName);

            if (!HasValue(element.GetType())) {
                throw new InvalidOperationException(
                    $"{GetType().Name}: '{elementName}' is a {element.GetType().Name}, which has no value to bind.");
            }

            return BindProperty(element, "value", viewModelProperty, mode);
        }

        /// <summary>
        /// Binds the <c>value</c> of <paramref name="field"/> to
        /// <paramref name="viewModelProperty"/>, two-way by default.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="field"/> is null.</exception>
        /// <exception cref="ArgumentException">As for <see cref="BindProperty"/>.</exception>
        protected DataBinding BindValue<TValue>(BaseField<TValue> field, string viewModelProperty,
            BindingMode mode = BindingMode.TwoWay) =>
            BindProperty(field, "value", viewModelProperty, mode);

        /// <summary>
        /// Runs <paramref name="command"/> when <paramref name="button"/> is clicked, and keeps the
        /// button enabled only while the command can execute.
        /// </summary>
        /// <remarks>
        /// The button follows <see cref="ICommand.CanExecuteChanged"/> through
        /// <see cref="VisualElement.SetEnabled"/>. Both subscriptions end when the view is
        /// disposed: the command often lives on a ViewModel that outlives the view (a child view's
        /// ViewModel, for one), and would otherwise keep the view alive and keep driving its button.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="button"/> or <paramref name="command"/> is null.</exception>
        protected void BindCommand(Button button, ICommand command)
        {
            if (button == null) {
                throw new ArgumentNullException(nameof(button));
            }

            if (command == null) {
                throw new ArgumentNullException(nameof(command));
            }

            if (IsDisposed) {
                return;
            }

            Action onClicked = () => {
                if (!IsDisposed && command.CanExecute) {
                    command.Execute();
                }
            };

            Action onCanExecuteChanged = () => {
                if (!IsDisposed) {
                    button.SetEnabled(command.CanExecute);
                }
            };

            button.clicked += onClicked;
            command.CanExecuteChanged += onCanExecuteChanged;
            button.SetEnabled(command.CanExecute);

            commandUnbinds ??= new List<Action>();
            commandUnbinds.Add(() => {
                button.clicked -= onClicked;
                command.CanExecuteChanged -= onCanExecuteChanged;
            });
        }

        /// <summary>
        /// Binds the <see cref="Button"/> named <paramref name="buttonName"/> to
        /// <paramref name="command"/>, as <see cref="BindCommand(Button, ICommand)"/> does.
        /// </summary>
        /// <exception cref="InvalidOperationException">There is no such button.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="command"/> is null.</exception>
        protected void BindCommand(string buttonName, ICommand command) =>
            BindCommand(Require<Button>(buttonName), command);

        /// <summary>
        /// Runs <paramref name="onChanged"/> each time the ViewModel raises a change of
        /// <paramref name="propertyName"/>; pass <c>nameof(TViewModel.Property)</c>.
        /// </summary>
        /// <remarks>
        /// It is not run on registration: it is meant for reactions to a change, such as an effect
        /// or an animation, not for state. Bind state with <see cref="BindProperty"/> or a UXML data
        /// binding instead, which also applies the current value. Callbacks for the same property
        /// run in registration order, before <see cref="OnPropertyChanged"/>, and never after
        /// <see cref="Dispose"/>. The property needs <c>[CreateProperty]</c>, as for a binding.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> or <paramref name="onChanged"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="propertyName"/> is empty or, in the editor or a
        /// development build, not a bindable property of the ViewModel.</exception>
        protected void Observe(string propertyName, Action onChanged)
        {
            ThrowIfInvalidObserver(propertyName, onChanged);

            if (IsDisposed) {
                return;
            }

            ValidatePath(propertyName, nameof(propertyName), false);
            AddObserver(propertyName, onChanged);
        }

        /// <summary>
        /// Like <see cref="Observe(string, Action)"/>, but passes the property's new value, read
        /// through Unity.Properties as a binding reads it.
        /// </summary>
        /// <remarks>
        /// <typeparamref name="T"/> must be the property's declared type or one it is assignable to
        /// (a base class, an interface, <c>object</c>). In the editor and in development builds a
        /// mismatch throws here. Release builds do not check: if the value cannot be read, the
        /// callback is skipped and an error is logged once for this registration.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> or <paramref name="onChanged"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="propertyName"/> is empty or, in the editor or a
        /// development build, not a bindable property of the ViewModel, or not of type <typeparamref name="T"/>.</exception>
        protected void Observe<T>(string propertyName, Action<T> onChanged)
        {
            ThrowIfInvalidObserver(propertyName, onChanged);

            if (IsDisposed) {
                return;
            }

            var property = ValidatePath(propertyName, nameof(propertyName), false);

            if (property != null && !typeof(T).IsAssignableFrom(property.DeclaredValueType())) {
                throw new ArgumentException(
                    $"{GetType().Name}: {ViewModel.GetType().Name}.{propertyName} is a " +
                    $"{property.DeclaredValueType().Name}, which Observe<{typeof(T).Name}> cannot receive.",
                    nameof(propertyName));
            }

            var path = new PropertyPath(propertyName);
            var reported = false;

            AddObserver(propertyName, () => {
                if (PropertyContainer.TryGetValue((object)ViewModel, path, out T value)) {
                    onChanged(value);
                } else if (!reported) {
                    reported = true;
                    Debug.LogError(
                        $"{GetType().Name}: cannot read {ViewModel.GetType().Name}.{propertyName} as a " +
                        $"{typeof(T).Name}, so its Observe callback is skipped. Check that the property is " +
                        "marked [CreateProperty] and of that type.");
                }
            });
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

            propertyChangedHandler = (_, args) => DispatchPropertyChanged(args.propertyName.ToString());
            ViewModel.propertyChanged += propertyChangedHandler;

            OnAttach();
        }

        private static void ThrowIfInvalidObserver(string propertyName, Delegate onChanged)
        {
            if (propertyName == null) {
                throw new ArgumentNullException(nameof(propertyName));
            }

            if (propertyName.Length == 0) {
                throw new ArgumentException("A property name is required.", nameof(propertyName));
            }

            if (onChanged == null) {
                throw new ArgumentNullException(nameof(onChanged));
            }
        }

        private static bool HasValue(Type elementType) =>
            elementType.GetInterfaces().Any(type =>
                type.IsGenericType && type.GetGenericTypeDefinition() == typeof(INotifyValueChanged<>));

        /// <summary>
        /// In the editor and development builds, resolves <paramref name="path"/> on the ViewModel
        /// and returns its property, or throws if there is none (or, with
        /// <paramref name="requireWritable"/>, if it is read-only). Release builds skip the check
        /// and return null.
        /// </summary>
        /// <remarks>
        /// Runs once, when the binding or observer is registered, not on each change. The path is
        /// resolved against the ViewModel's runtime type, which may be a subclass of
        /// <typeparamref name="TViewModel"/>: passing the container as <c>object</c> makes
        /// Unity.Properties pick the property bag of the instance rather than of the static type.
        /// </remarks>
        private IProperty ValidatePath(string path, string parameterName, bool requireWritable)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentException("A ViewModel property is required.", parameterName);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ViewModel == null) {
                return null;
            }

            var viewModelType = ViewModel.GetType().Name;

            if (!PropertyContainer.TryGetProperty((object)ViewModel, new PropertyPath(path), out var property)) {
                throw new ArgumentException(
                    $"{GetType().Name}: {viewModelType} has no bindable property '{path}'. Check the name, " +
                    "and that the property is marked [CreateProperty].", parameterName);
            }

            if (requireWritable && property.IsReadOnly) {
                throw new ArgumentException(
                    $"{GetType().Name}: {viewModelType}.{path} is read-only, so it cannot be bound in a mode " +
                    "that writes to the ViewModel. Give it a setter, or bind it ToTarget.", parameterName);
            }

            return property;
#else
            return null;
#endif
        }

        private void AddObserver(string propertyName, Action callback)
        {
            observers ??= new Dictionary<string, List<Action>>();

            if (!observers.TryGetValue(propertyName, out var callbacks)) {
                callbacks = new List<Action>();
                observers.Add(propertyName, callbacks);
            }

            callbacks.Add(callback);
        }

        private void UnbindCommands()
        {
            if (commandUnbinds == null) {
                return;
            }

            var unbinds = commandUnbinds;
            commandUnbinds = null;

            foreach (var unbind in unbinds) {
                unbind();
            }
        }

        // A callback may dispose the view; nothing runs after that.
        private void DispatchPropertyChanged(string propertyName)
        {
            if (observers != null && observers.TryGetValue(propertyName, out var callbacks)) {
                var count = callbacks.Count;

                for (var i = 0; i < count && !IsDisposed; i++) {
                    callbacks[i]();
                }
            }

            if (!IsDisposed) {
                OnPropertyChanged(propertyName);
            }
        }
    }
}
