using System;

namespace UIKit
{
    /// <summary>
    /// A set of screens registered together and torn down together. Disposing the group closes
    /// its open screens - without hide animations - and unregisters them.
    /// </summary>
    /// <remarks>
    /// DI integrations create one group per container scope, so screens registered in a child
    /// scope resolve their ViewModels from it and close when it is disposed. Without DI, use
    /// one group per feature or scene, or just <see cref="UIKitContext.Register{TViewModel,TView}(Func{TViewModel},Func{TView})"/>.
    /// </remarks>
    public sealed class ScreenGroup : IDisposable
    {
        private readonly ScreenRegistry screenRegistry;
        private readonly ScreenNavigator screenNavigator;

        public bool IsDisposed { get; private set; }

        internal ScreenGroup(ScreenRegistry screenRegistry, ScreenNavigator screenNavigator)
        {
            this.screenRegistry = screenRegistry;
            this.screenNavigator = screenNavigator;
        }

        /// <summary>
        /// Registers a screen. Both factories are called each time the screen opens.
        /// </summary>
        public ScreenGroup Register<TViewModel, TView>(Func<TViewModel> createViewModel, Func<TView> createView)
            where TViewModel : ViewModel
            where TView : View<TViewModel>
        {
            if (createViewModel == null) {
                throw new ArgumentNullException(nameof(createViewModel));
            }

            if (createView == null) {
                throw new ArgumentNullException(nameof(createView));
            }

            if (IsDisposed) {
                throw new ObjectDisposedException(nameof(ScreenGroup));
            }

            screenRegistry.Add(typeof(TViewModel), new ScreenBinding(
                () => createViewModel(),
                () => createView(),
                this));

            return this;
        }

        /// <summary>
        /// Registers a screen whose view has a parameterless constructor.
        /// </summary>
        public ScreenGroup Register<TViewModel, TView>(Func<TViewModel> createViewModel)
            where TViewModel : ViewModel
            where TView : View<TViewModel>, new()
        {
            return Register(createViewModel, () => new TView());
        }

        /// <summary>
        /// Registers a view that is only ever created as a child of another view, through
        /// <see cref="View{TViewModel}.ViewFactory"/>. Its parent supplies the ViewModel, so
        /// there is no ViewModel factory, and the navigator refuses to open it as a screen.
        /// </summary>
        public ScreenGroup RegisterView<TViewModel, TView>(Func<TView> createView)
            where TViewModel : ViewModel
            where TView : View<TViewModel>
        {
            if (createView == null) {
                throw new ArgumentNullException(nameof(createView));
            }

            if (IsDisposed) {
                throw new ObjectDisposedException(nameof(ScreenGroup));
            }

            screenRegistry.Add(typeof(TViewModel), new ScreenBinding(null, () => createView(), this));
            return this;
        }

        /// <summary>
        /// Registers a child view that has a parameterless constructor; see
        /// <see cref="RegisterView{TViewModel,TView}(Func{TView})"/>.
        /// </summary>
        public ScreenGroup RegisterView<TViewModel, TView>()
            where TViewModel : ViewModel
            where TView : View<TViewModel>, new()
        {
            return RegisterView<TViewModel, TView>(() => new TView());
        }

        public void Dispose()
        {
            if (IsDisposed) {
                return;
            }

            IsDisposed = true;

            // Open screens first: they are what still holds the group's services. Dropping the
            // registrations first would leave them running with no record of where they came from.
            screenNavigator.DisposeScreensOwnedBy(this);
            screenRegistry.RemoveOwnedBy(this);
        }
    }
}
