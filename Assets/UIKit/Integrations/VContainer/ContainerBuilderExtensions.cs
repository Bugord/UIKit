using System;
using UnityEngine.UIElements;
using VContainer;

namespace UIKit.VContainer
{
    public static class ContainerBuilderExtensions
    {
        /// <summary>
        /// Installs UIKit with its own panel. Call this once, in the scope that should own the UI
        /// root and the navigation stack - typically the root scope. Screens may be registered
        /// here or in any descendant scope; see <see cref="RegisterScreen{TViewModel,TView}"/>.
        /// </summary>
        public static void AddUIKit(this IContainerBuilder builder, PanelSettings panelSettings,
            params ScreenCatalog[] catalogs)
        {
            if (panelSettings == null) {
                throw new ArgumentNullException(nameof(panelSettings));
            }

            AddUIKit(builder, _ => new UIKitContext(panelSettings, catalogs));
        }

        /// <summary>
        /// Installs UIKit hosted by <paramref name="rootProvider"/>, for example a
        /// <see cref="VisualElementRootProvider"/> inside your own UIDocument.
        /// </summary>
        public static void AddUIKit(this IContainerBuilder builder, IUIRootProvider rootProvider,
            params ScreenCatalog[] catalogs)
        {
            if (rootProvider == null) {
                throw new ArgumentNullException(nameof(rootProvider));
            }

            AddUIKit(builder, _ => new UIKitContext(rootProvider, catalogs));
        }

        /// <summary>
        /// Registers a screen in the scope being configured. This need not be the scope that
        /// called <c>AddUIKit</c>: a screen registered in a child scope resolves its ViewModel
        /// from there, so the ViewModel can depend on that scope's services, and it is
        /// force-closed and unregistered when the scope is disposed. The UI root and the
        /// navigation stack stay single and shared regardless of where screens are registered.
        /// </summary>
        public static void RegisterScreen<TViewModel, TView>(this IContainerBuilder builder)
            where TViewModel : ViewModel
            where TView : View<TViewModel>
        {
            // Transient, and so not tracked by the container: the view disposes its ViewModel,
            // and the navigator disposes the view.
            builder.Register<TViewModel>(Lifetime.Transient);
            builder.Register<TView>(Lifetime.Transient);

            if (!builder.Exists(typeof(ScreenScope))) {
                // Scoped rather than Singleton: each LifetimeScope registering screens needs its
                // own group, and VContainer disposes Scoped instances with the scope that created
                // them - which is exactly when its screens have to close.
                builder.Register<ScreenScope>(Lifetime.Scoped);
            }

            // Deferred to build time because the factories have to close over the finished
            // container: resolving through this scope's resolver is what gives the ViewModel
            // access to this scope's registrations, and a builder cannot hand one out before it
            // has built.
            builder.RegisterBuildCallback(resolver =>
                resolver.Resolve<ScreenScope>().Group.Register(
                    () => resolver.Resolve<TViewModel>(),
                    () => resolver.Resolve<TView>()));
        }

        /// <summary>
        /// Registers a view that is only ever created as a child of another view. Its parent
        /// supplies the ViewModel, so only the view is registered in the container.
        /// </summary>
        public static void RegisterView<TViewModel, TView>(this IContainerBuilder builder)
            where TViewModel : ViewModel
            where TView : View<TViewModel>
        {
            builder.Register<TView>(Lifetime.Transient);

            if (!builder.Exists(typeof(ScreenScope))) {
                builder.Register<ScreenScope>(Lifetime.Scoped);
            }

            builder.RegisterBuildCallback(resolver =>
                resolver.Resolve<ScreenScope>().Group.RegisterView<TViewModel, TView>(() => resolver.Resolve<TView>()));
        }

        private static void AddUIKit(IContainerBuilder builder, Func<IObjectResolver, UIKitContext> createContext)
        {
            // Registered as a factory so the container owns the context and disposes it with
            // the scope.
            builder.Register(createContext, Lifetime.Singleton);
            builder.Register(resolver => resolver.Resolve<UIKitContext>().Navigator, Lifetime.Singleton);
            builder.Register(resolver => resolver.Resolve<UIKitContext>().ViewFactory, Lifetime.Singleton);
        }
    }
}
