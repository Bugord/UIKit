using UIKit.Runtime.Abstractions;

namespace UIKit.Runtime.Core
{
    public class ActiveScreen
    {
        public readonly ViewModel ViewModel;
        public readonly IView View;

        public ActiveScreen(ViewModel viewModel, IView view)
        {
            ViewModel = viewModel;
            View = view;
        }
    }
}