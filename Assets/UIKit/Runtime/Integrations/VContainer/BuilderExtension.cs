using UIKit.Runtime.Abstractions;
using UIKit.Runtime.Core;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;

namespace UIKit.Runtime.Integrations.VContainer
{
    public static class BuilderExtension
    {
        /// <summary>
        /// Installs UIKit. Call this once, in the scope that should own the UI root and the
        /// navigation stack - typically the root scope. Screens themselves may be registered
        /// here or in any descendant scope; see
        /// <see cref="RegisterScreen{TViewModel,TView}"/>.
        /// </summary>
        public static void AddUIKit(this IContainerBuilder builder, ScreenCatalog screenCatalog, PanelSettings panelSettings = null)
        {
            builder.RegisterInstance(screenCatalog);

            panelSettings ??= ScriptableObject.CreateInstance<PanelSettings>();
            builder.RegisterInstance(panelSettings);

            builder.Register<UIRootProvider>(Lifetime.Singleton).As<IUIRootProvider>().AsSelf();
            builder.Register<UIRootProviderInitializer>(Lifetime.Singleton).As<IInitializable>();

            builder.Register<ScreenRegistry>(Lifetime.Singleton).AsSelf();
            builder.Register<ViewFactory>(Lifetime.Singleton).As<IViewFactory>();

            // AsSelf as well as the interface: ScreenScope needs the concrete navigator to
            // evict its screens, which is infrastructure rather than app-facing API.
            builder.Register<ScreenNavigator>(Lifetime.Singleton).As<IScreenNavigator>().AsSelf();
        }

        /// <summary>
        /// Registers a screen in the scope being configured. This need not be the scope that
        /// called <see cref="AddUIKit"/>: a screen registered in a child scope resolves its
        /// ViewModel from there, so the ViewModel can depend on that scope's services, and it
        /// is force-closed and unregistered when the scope is disposed. The UI root and the
        /// navigation stack stay single and shared regardless of where screens are registered.
        /// </summary>
        public static void RegisterScreen<TViewModel, TView>(this IContainerBuilder builder)
            where TViewModel : ViewModel
            where TView : View<TViewModel>
        {
            builder.Register<TViewModel>(Lifetime.Transient);
            builder.Register<TView>(Lifetime.Transient).AsSelf();

            if (!builder.Exists(typeof(ScreenScope))) {
                // Scoped rather than Singleton: each LifetimeScope registering screens needs
                // its own owner token, and VContainer disposes Scoped instances with the scope
                // that created them - which is exactly when the eviction has to happen.
                builder.Register<ScreenScope>(Lifetime.Scoped).AsSelf();
            }

            // Deferred to build time because the binding has to close over the finished
            // container. That is the whole point: resolving through this scope's resolver is
            // what gives the ViewModel access to this scope's registrations, and a builder
            // cannot hand one out before it has built.
            builder.RegisterBuildCallback(resolver =>
                resolver.Resolve<ScreenScope>().Register<TViewModel, TView>(resolver));
        }
    }
}
