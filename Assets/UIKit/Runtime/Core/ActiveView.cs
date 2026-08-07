using UIKit.Runtime.Abstractions;

namespace UIKit.Runtime.Core
{
    public class ActiveView
    {
        public readonly ViewModel ViewModel;
        public readonly IView View;

        /// <summary>
        /// The scope this screen was resolved from, carried over from its
        /// <see cref="ScreenBinding"/> so the navigator can close it when that scope dies.
        /// </summary>
        public readonly object Owner;

        public ActiveView(ViewModel viewModel, IView view, object owner)
        {
            ViewModel = viewModel;
            View = view;
            Owner = owner;
        }
    }
}
