using UIKit.Runtime.Abstractions;

namespace UIKit.Runtime.Core
{
    public class ActiveView
    {
        public readonly ViewModel ViewModel;
        public readonly IView View;

        public ActiveView(ViewModel viewModel, IView view)
        {
            ViewModel = viewModel;
            View = view;
        }
    }
}