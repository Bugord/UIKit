using UIKit.Runtime.Abstractions;
using UIKit.Runtime.Core;
using UIKit.Runtime.Widgets;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;

namespace UIKit.Runtime.Integrations.VContainer
{
    public static class BuilderExtension
    {
        public static void AddUIKit(this IContainerBuilder builder, ScreenCatalog screenCatalog,
            WidgetCatalog widgetCatalog = null, PanelSettings panelSettings = null)
        {
            builder.RegisterInstance(screenCatalog);
            if (widgetCatalog != null) {
                builder.RegisterInstance(widgetCatalog);
            }

            builder.RegisterInstance(panelSettings != null ? panelSettings : ScriptableObject.CreateInstance<PanelSettings>());

            builder.Register<UIRootProvider>(Lifetime.Singleton)
                .As<IUIRootProvider>()
                .As<IInitializable>();

            builder.Register<ScreenResolver>(Lifetime.Singleton).As<IScreenResolver>();
            builder.Register<ScreenNavigator>(Lifetime.Singleton).As<IScreenNavigator>();
            builder.Register<WidgetFactory>(Lifetime.Singleton).As<IWidgetFactory>();
            builder.Register<WidgetHost>(Lifetime.Transient);
        }

        public static void RegisterScreen<TViewModel, TView>(this IContainerBuilder builder)
            where TViewModel : ViewModel
            where TView : View<TViewModel>
        {
            builder.Register<TViewModel>(Lifetime.Transient);
            builder.Register<TView>(Lifetime.Transient).As<View<TViewModel>>();
        }

        public static void RegisterWidget<TViewModel, TWidget>(this IContainerBuilder builder)
            where TViewModel : ViewModel
            where TWidget : IWidget
        {
            builder.Register<TViewModel>(Lifetime.Transient);
            builder.Register<TWidget>(Lifetime.Transient);
        }
    }
}