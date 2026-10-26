using UIKit;
using Zenject;

namespace Example.Zenject
{
    public sealed class ExampleEntryPoint : IInitializable
    {
        private readonly IScreenNavigator navigator;

        public ExampleEntryPoint(IScreenNavigator navigator)
        {
            this.navigator = navigator;
        }

        public void Initialize()
        {
            navigator.OpenAsync<MainMenuViewModel>().Forget();
        }
    }
}
