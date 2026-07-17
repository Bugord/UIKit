using UIKit.Runtime.Abstractions;
using UIKit.Runtime.Core;
using VContainer;

namespace UIKit.Runtime.Integrations.VContainer
{
    public class VContainerScreenResolver : IScreenResolver
    {
        private readonly IObjectResolver resolver;

        public VContainerScreenResolver(IObjectResolver resolver)
        {
            this.resolver = resolver;
        }

        public TViewModel Resolve<TViewModel>() where TViewModel : ViewModel
        {
            return resolver.Resolve<TViewModel>();
        }

        public TView ResolveView<TView, TViewModel>()
            where TView : View<TViewModel>
            where TViewModel : ViewModel
        {
            return resolver.Resolve<TView>();
        }
    }
}
