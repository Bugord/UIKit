using UIKit.Runtime.Abstractions;
using VContainer;

namespace UIKit.Runtime.Core
{
    public class ScreenResolver : IScreenResolver
    {
        private readonly IObjectResolver resolver;

        public ScreenResolver(IObjectResolver resolver)
        {
            this.resolver = resolver;
        }

        public TViewModel Resolve<TViewModel>() where TViewModel : ViewModel
        {
            return resolver.Resolve<TViewModel>();
        }

        public View<TViewModel> ResolveView<TViewModel>()
            where TViewModel : ViewModel
        {
            return resolver.Resolve<View<TViewModel>>();
        }
    }
}