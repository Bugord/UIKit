using UIToolkitMVVM.Navigation;
using UIToolkitMVVM.Views;
using VContainer;

namespace UIToolkitMVVM.VContainer
{
    public class VContainerUIScreenFactory : IUIScreenFactory
    {
        private readonly IObjectResolver resolver;
        public VContainerUIScreenFactory(IObjectResolver resolver) => this.resolver = resolver;
        public TController Create<TController>() where TController : IView => resolver.Resolve<TController>();
    }
}
