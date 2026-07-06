using UIKit.Runtime.Core;
using UIKit.Runtime.Widgets;
using VContainer;

namespace UIKit.Runtime.Integrations.VContainer
{
    public class WidgetFactory : IWidgetFactory
    {
        private readonly IObjectResolver resolver;

        public WidgetFactory(IObjectResolver resolver)
        {
            this.resolver = resolver;
        }

        public TWidget Create<TWidget>() where TWidget : IWidget
        {
            return resolver.Resolve<TWidget>();
        }
    }
}