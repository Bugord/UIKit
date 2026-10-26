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
    /// <see cref="SetProperty{T}"/> or <see cref="Notify"/>.
    /// </remarks>
    public abstract class ViewModel : IDisposable, INotifyBindablePropertyChanged
    {
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
        /// <see cref="propertyChanged"/> if the value changed.
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
        /// a computed property whose inputs changed.
        /// </summary>
        protected void Notify([CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName)) {
                throw new ArgumentException("A property name is required.", nameof(propertyName));
            }

            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(propertyName));
        }
    }
}
