using Cysharp.Threading.Tasks;

namespace UIKit.Runtime.Abstractions
{
    public class ScreenHandle<TViewModel> where TViewModel : ViewModel
    {
        private readonly IScreenNavigator navigator;

        public TViewModel ViewModel { get; }
        public bool IsOpen => navigator.IsOpen(ViewModel);

        internal ScreenHandle(IScreenNavigator navigator, TViewModel viewModel)
        {
            this.navigator = navigator;
            ViewModel = viewModel;
        }

        public UniTask CloseAsync() => IsOpen ? UniTask.CompletedTask : navigator.CloseAsync(ViewModel);
    }
}