using UnityEngine;

namespace UIKit
{
    /// <summary>
    /// An open screen, as returned by <see cref="IScreenNavigator.OpenAsync{TViewModel}"/>.
    /// </summary>
    public sealed class ScreenHandle<TViewModel> where TViewModel : ViewModel
    {
        private readonly IScreenNavigator navigator;

        public TViewModel ViewModel { get; }
        public bool IsOpen => navigator.IsOpen(ViewModel);

        public ScreenHandle(IScreenNavigator navigator, TViewModel viewModel)
        {
            this.navigator = navigator;
            ViewModel = viewModel;
        }

        /// <summary>
        /// Closes the screen. Does nothing if it is already closed.
        /// </summary>
        public Awaitable CloseAsync() => navigator.CloseAsync(ViewModel);
    }
}
