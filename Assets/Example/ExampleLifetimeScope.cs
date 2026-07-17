using Example.Confirmation;
using Example.Profile;
using Example.Rename;
using Example.Settings;
using UIKit.Runtime.Core;
using UIKit.Runtime.Integrations.VContainer;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;

namespace Example
{
    public class ExampleLifetimeScope : LifetimeScope
    {
        [SerializeField] private ScreenCatalog screenCatalog;
        [SerializeField] private PanelSettings panelSettings;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.AddUIKit(screenCatalog, panelSettings);

            builder.RegisterScreen<MainMenuViewModel, MainMenuView>();
            builder.RegisterScreen<ConfirmPopupViewModel, ConfirmPopupView>();
            builder.RegisterScreen<SettingsViewModel, SettingsView>();
            builder.RegisterScreen<ProfileViewModel, ProfileView>();
            builder.RegisterScreen<NameInputPopupViewModel, NameInputPopupView>();

            builder.RegisterEntryPoint<ExampleEntryPoint>();
        }
    }
}
