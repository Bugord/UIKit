using VContainer;

namespace UI.Widgets
{
    public class VContainerWidgetsFactory : IWidgetsFactory
    {
        private readonly IObjectResolver resolver;
        public VContainerWidgetsFactory(IObjectResolver resolver) => this.resolver = resolver;
        public TWidget Create<TWidget>() where TWidget : IWidget => resolver.Resolve<TWidget>();
    }
}