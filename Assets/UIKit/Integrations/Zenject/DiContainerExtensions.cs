using System;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;
using Zenject;

namespace UIKit.Zenject
{
    public static class DiContainerExtensions
    {
        // Containers that already bound their ScreenScope. Weak so a disposed container is not
        // kept alive by this table.
        private static readonly ConditionalWeakTable<DiContainer, object> ScopedContainers = new();

        /// <summary>
        /// Installs UIKit with its own panel. Call this once, in the container that should own
        /// the UI root and the navigation stack - typically the ProjectContext or SceneContext.
        /// Screens may be registered here or in any subcontainer; see
        /// <see cref="RegisterScreen{TViewModel,TView}"/>.
        /// </summary>
        public static void BindUIKit(this DiContainer container, PanelSettings panelSettings,
            params ScreenCatalog[] catalogs)
        {
            if (panelSettings == null) {
                throw new ArgumentNullException(nameof(panelSettings));
            }

            BindUIKit(container, () => new UIKitContext(panelSettings, catalogs));
        }

        /// <summary>
        /// Installs UIKit hosted by <paramref name="rootProvider"/>, for example a
        /// <see cref="VisualElementRootProvider"/> inside your own UIDocument.
        /// </summary>
        public static void BindUIKit(this DiContainer container, IUIRootProvider rootProvider,
            params ScreenCatalog[] catalogs)
        {
            if (rootProvider == null) {
                throw new ArgumentNullException(nameof(rootProvider));
            }

            BindUIKit(container, () => new UIKitContext(rootProvider, catalogs));
        }

        /// <summary>
        /// Registers a screen in <paramref name="container"/>. A screen registered in a
        /// subcontainer resolves its ViewModel from there, so the ViewModel can depend on that
        /// container's bindings, and it is force-closed and unregistered when the subcontainer
        /// is disposed. The UI root and the navigation stack stay single and shared.
        /// </summary>
        public static void RegisterScreen<TViewModel, TView>(this DiContainer container)
            where TViewModel : ViewModel
            where TView : View<TViewModel>
        {
            // Transient: the view disposes its ViewModel, and the navigator disposes the view.
            container.Bind<TViewModel>().AsTransient();
            container.Bind<TView>().AsTransient();

            container.BindInstance(new ScreenRegistration((group, resolver) =>
                group.Register(() => resolver.Resolve<TViewModel>(), () => resolver.Resolve<TView>())));

            BindScreenScopeOnce(container);
        }

        /// <summary>
        /// Registers a view that is only ever created as a child of another view. Its parent
        /// supplies the ViewModel, so only the view is bound in the container.
        /// </summary>
        public static void RegisterView<TViewModel, TView>(this DiContainer container)
            where TViewModel : ViewModel
            where TView : View<TViewModel>
        {
            container.Bind<TView>().AsTransient();

            container.BindInstance(new ScreenRegistration((group, resolver) =>
                group.RegisterView<TViewModel, TView>(() => resolver.Resolve<TView>())));

            BindScreenScopeOnce(container);
        }

        private static void BindScreenScopeOnce(DiContainer container)
        {
            if (ScopedContainers.TryGetValue(container, out _)) {
                return;
            }

            ScopedContainers.Add(container, null);

            // NonLazy so the screens are registered as soon as the container is built, before any
            // IInitializable runs and might open one; bound to IDisposable so the container's
            // DisposableManager closes them with it.
            container.BindInterfacesAndSelfTo<ScreenScope>().AsSingle().NonLazy();
        }

        private static void BindUIKit(DiContainer container, Func<UIKitContext> createContext)
        {
            // Bound to IDisposable as well, so the DisposableManager disposes the context with
            // the container.
            container.BindInterfacesAndSelfTo<UIKitContext>().FromMethod(_ => createContext()).AsSingle();
            container.Bind<IScreenNavigator>().FromResolveGetter<UIKitContext>(context => context.Navigator).AsSingle();
            container.Bind<IViewFactory>().FromResolveGetter<UIKitContext>(context => context.ViewFactory).AsSingle();
        }
    }
}
