using UIKit.Runtime.Core;
using VContainer.Unity;

namespace UIKit.Runtime.Integrations.VContainer
{
    internal sealed class UIRootProviderInitializer : IInitializable
    {
        private readonly UIRootProvider uiRootProvider;

        public UIRootProviderInitializer(UIRootProvider uiRootProvider)
        {
            this.uiRootProvider = uiRootProvider;
        }

        public void Initialize() => uiRootProvider.Initialize();
    }
}
