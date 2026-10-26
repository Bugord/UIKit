using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIKit
{
    /// <summary>
    /// Base class for the state and logic behind a view.
    /// </summary>
    /// <remarks>
    /// Implements <see cref="INotifyBindablePropertyChanged"/>, and its view sets it as the
    /// root element's <c>dataSource</c>, so UI Toolkit data bindings declared in UXML resolve
    /// against it. Mark bound properties with <c>[CreateProperty]</c> and raise changes through
    /// <see cref="SetProperty{T}"/> or <see cref="Notify(string)"/>. A computed property marked
    /// <see cref="DependsOnAttribute"/> is notified whenever one of its inputs is.
    /// </remarks>
    public abstract class ViewModel : IDisposable, INotifyBindablePropertyChanged
    {
        private readonly PropertyDependencies dependencies;

        private CancellationTokenSource lifetime;

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Cancelled when this ViewModel is disposed - pass it to work that should not outlive
        /// the screen.
        /// </summary>
        public CancellationToken LifetimeToken
        {
            get {
                if (IsDisposed) {
                    return new CancellationToken(true);
                }

                lifetime ??= new CancellationTokenSource();
                return lifetime.Token;
            }
        }

        /// <exception cref="InvalidOperationException">A <see cref="DependsOnAttribute"/> of this type names an unknown property, or the dependencies form a cycle.</exception>
        protected ViewModel()
        {
            dependencies = PropertyDependencies.For(GetType());
        }

        /// <summary>
        /// Disposes this ViewModel. Safe to call more than once; only the first call runs
        /// <see cref="OnDispose"/>.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed) {
                return;
            }

            IsDisposed = true;

            try {
                lifetime?.Cancel();
            } finally {
                lifetime?.Dispose();
                lifetime = null;

                try {
                    OnDisposing();
                    OnDispose();
                } finally {
                    propertyChanged = null;
                }
            }
        }

        /// <summary>
        /// Called by the navigator once this screen has been shown.
        /// </summary>
        protected internal virtual void OnOpened()
        {
        }

        /// <summary>
        /// Called by the navigator when this screen starts closing, before its hide animation.
        /// Not called when the screen is force-closed because its <see cref="ScreenGroup"/> or
        /// the <see cref="UIKitContext"/> is disposed; <see cref="OnDispose"/> runs either way.
        /// </summary>
        protected internal virtual void OnClosed()
        {
        }

        protected virtual void OnDispose()
        {
        }

        // Framework cleanup that must run even when a subclass overrides OnDispose without
        // calling base.
        private protected virtual void OnDisposing()
        {
        }

        /// <summary>
        /// Assigns <paramref name="value"/> to <paramref name="field"/> and raises
        /// <see cref="propertyChanged"/> through <see cref="Notify(string)"/> if the value changed,
        /// so the properties that depend on it are notified too.
        /// </summary>
        /// <returns>Whether the value changed.</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) {
                return false;
            }

            field = value;
            Notify(propertyName);
            return true;
        }

        /// <summary>
        /// Raises <see cref="propertyChanged"/> for <paramref name="propertyName"/>, e.g. for
        /// a computed property whose inputs changed, and then for the properties that depend on
        /// it through <see cref="DependsOnAttribute"/>.
        /// </summary>
        /// <remarks>
        /// The name is not checked against the ViewModel's properties: a view may observe any
        /// name. Dependents are raised breadth-first, each once, in declaration order within a
        /// level (base class first).
        /// </remarks>
        protected void Notify([CallerMemberName] string propertyName = null)
        {
            ThrowIfNullOrEmpty(propertyName, nameof(propertyName));

            if (!dependencies.HasDependents(propertyName)) {
                Raise(propertyName);
                return;
            }

            NotifyWithDependents(new List<string> { propertyName });
        }

        /// <summary>
        /// Raises <see cref="propertyChanged"/> for each of the given properties, in argument
        /// order, and then for their dependents (see <see cref="Notify(string)"/>). Each property
        /// is raised at most once per call, even when it is named twice or reached from several
        /// sources.
        /// </summary>
        /// <remarks>
        /// For a ViewModel that shows state it does not own: when that state changes, notify every
        /// property computed from it in one call.
        /// </remarks>
        protected void Notify(string first, string second, params string[] rest)
        {
            ThrowIfNullOrEmpty(first, nameof(first));
            ThrowIfNullOrEmpty(second, nameof(second));

            var sources = new List<string>(2 + (rest?.Length ?? 0)) { first, second };

            if (rest != null) {
                foreach (var name in rest) {
                    ThrowIfNullOrEmpty(name, nameof(rest));
                    sources.Add(name);
                }
            }

            NotifyWithDependents(sources);
        }

        private void NotifyWithDependents(List<string> sources)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var order = new List<string>(sources.Count);

            foreach (var source in sources) {
                if (seen.Add(source)) {
                    order.Add(source);
                }
            }

            dependencies.AppendDependents(order, seen);

            foreach (var name in order) {
                Raise(name);
            }
        }

        private void Raise(string propertyName) =>
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(propertyName));

        private static void ThrowIfNullOrEmpty(string propertyName, string parameterName)
        {
            if (string.IsNullOrEmpty(propertyName)) {
                throw new ArgumentException("A property name is required.", parameterName);
            }
        }
    }
}
