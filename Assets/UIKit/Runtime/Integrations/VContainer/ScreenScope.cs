using System;
using UIKit.Runtime.Abstractions;
using UIKit.Runtime.Core;
using VContainer;

namespace UIKit.Runtime.Integrations.VContainer
{
    /// <summary>
    /// Owns the screens registered by one <c>LifetimeScope</c>. One instance is created per
    /// scope that calls <see cref="BuilderExtension.RegisterScreen{TViewModel,TView}"/>, and
    /// VContainer disposes it together with that scope - which is when the scope's screens are
    /// closed and its registry entries removed.
    /// </summary>
    public sealed class ScreenScope : IDisposable
    {
        private readonly ScreenRegistry screenRegistry;
        private readonly ScreenNavigator screenNavigator;

        public ScreenScope(ScreenRegistry screenRegistry, ScreenNavigator screenNavigator)
        {
            this.screenRegistry = screenRegistry;
            this.screenNavigator = screenNavigator;
        }

        /// <param name="resolver">
        /// The container this scope built. Capturing it - rather than the one the navigator
        /// was registered in - is what lets the ViewModel take this scope's services: a child
        /// container resolves its own registrations first and falls through to its parents.
        /// </param>
        public void Register<TViewModel, TView>(IObjectResolver resolver)
            where TViewModel : ViewModel
            where TView : View<TViewModel>
        {
            screenRegistry.Add(typeof(TViewModel), new ScreenBinding(
                () => resolver.Resolve<TViewModel>(),
                () => resolver.Resolve<TView>(),
                this));
        }

        public void Dispose()
        {
            // Open screens first: they are what still holds this scope's services. Dropping
            // the registrations first would leave them running against a dead container with
            // no record of where they came from.
            screenNavigator.DisposeScreensOwnedBy(this);
            screenRegistry.RemoveOwnedBy(this);
        }
    }
}
