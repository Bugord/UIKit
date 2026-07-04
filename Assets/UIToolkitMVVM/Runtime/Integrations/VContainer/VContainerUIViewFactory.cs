using UIToolkitMVVM.Navigation;
using UIToolkitMVVM.Views;
using VContainer;

namespace UIToolkitMVVM.VContainer
{
    public class VContainerUIViewFactory : IUIViewFactory
    {
        private readonly IObjectResolver resolver;
        public VContainerUIViewFactory(IObjectResolver resolver) => this.resolver = resolver;
        public TController Create<TController>() where TController : IView => resolver.Resolve<TController>();
    }
}
