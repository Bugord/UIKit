using UIKit;
using VContainer.Unity;

namespace Example.VContainer
{
    public sealed class ExampleEntryPoint : IStartable
    {
        private readonly IScreenNavigator navigator;

        public ExampleEntryPoint(IScreenNavigator navigator)
        {
            this.navigator = navigator;
        }

        public void Start()
        {
            navigator.OpenAsync<MainMenuViewModel>().Forget();
        }
    }
}
