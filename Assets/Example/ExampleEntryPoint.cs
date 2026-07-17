using Cysharp.Threading.Tasks;
using UIKit.Runtime.Abstractions;
using VContainer.Unity;

namespace Example
{
    public class ExampleEntryPoint : IStartable
    {
        private readonly IScreenNavigator navigator;

        public ExampleEntryPoint(IScreenNavigator navigator)
        {
            this.navigator = navigator;
        }

        public void Start()
        {
            OpenMainMenuAsync().Forget();
        }

        private async UniTaskVoid OpenMainMenuAsync()
        {
            await navigator.OpenAsync<MainMenuViewModel>();
        }
    }
}
