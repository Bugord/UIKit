using UIToolkitMVVM.Widgets;
using VContainer;

namespace UIToolkitMVVM.VContainer
{
    public class VContainerWidgetFactory : IWidgetFactory
    {
        private readonly IObjectResolver resolver;
        public VContainerWidgetFactory(IObjectResolver resolver) => this.resolver = resolver;
        public TWidget Create<TWidget>() where TWidget : IWidget => resolver.Resolve<TWidget>();
    }
}