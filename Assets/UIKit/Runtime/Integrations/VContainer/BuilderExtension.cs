using UIKit.Runtime.Abstractions;
using UIKit.Runtime.Core;
using VContainer;

namespace UIKit.Runtime.Integrations.VContainer
{
    public static class BuilderExtension
    {
        public static void RegisterScreen<TViewModel, TView>(this IContainerBuilder builder)
            where TViewModel : ViewModel
            where TView : View<TViewModel>
        {
            builder.Register<TViewModel>(Lifetime.Transient);
            builder.Register<TView>(Lifetime.Transient).As<View<TViewModel>>();
        }
    }
}