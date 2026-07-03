using VContainer;

namespace UI.Navigation
{
    public class VContainerUIScreenFactory : IUIScreenFactory
    {
        private readonly IObjectResolver resolver;
        public VContainerUIScreenFactory(IObjectResolver resolver) => this.resolver = resolver;
        public TController Create<TController>() where TController : IView => resolver.Resolve<TController>();
    }
}
