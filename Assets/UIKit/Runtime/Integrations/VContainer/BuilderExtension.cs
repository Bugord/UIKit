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
        public static void AddUIKit(this IContainerBuilder builder, ScreenCatalog screenCatalog, PanelSettings panelSettings = null)
        {
            builder.RegisterInstance(screenCatalog);

            panelSettings ??= ScriptableObject.CreateInstance<PanelSettings>();
            builder.RegisterInstance(panelSettings);

            builder.Register<UIRootProvider>(Lifetime.Singleton).As<IUIRootProvider>().AsSelf();
            builder.Register<UIRootProviderInitializer>(Lifetime.Singleton).As<IInitializable>();

            builder.Register<VContainerScreenResolver>(Lifetime.Singleton).As<IScreenResolver>();
            builder.Register<ViewFactory>(Lifetime.Singleton).As<IViewFactory>();
            builder.Register<ScreenNavigator>(Lifetime.Singleton).As<IScreenNavigator>();
        }

        public static void RegisterScreen<TViewModel, TView>(this IContainerBuilder builder)
            where TViewModel : ViewModel
            where TView : View<TViewModel>
        {
            builder.Register<TViewModel>(Lifetime.Transient);
            builder.Register<TView>(Lifetime.Transient).AsSelf().As<View<TViewModel>>();
        }
    }
}