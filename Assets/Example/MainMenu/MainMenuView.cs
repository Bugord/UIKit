using UIKit.Runtime.Core;

namespace Example
{
    public class MainMenuView : View<MainMenuViewModel>
    {
        public MainMenuView(MainMenuViewModel viewModel) : base(viewModel)
        {
        }
    }
}