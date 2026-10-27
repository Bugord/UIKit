using Example.Confirmation;
using Example.Profile;
using Example.Rename;
using Example.Settings;
using UIKit;
using UIKit.VContainer;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;

namespace Example.VContainer
{
    /// <summary>
    /// The same example as ExampleBootstrap, wired with VContainer: the container builds the
    /// ViewModels, so their constructor dependencies are resolved instead of passed by hand.
    /// </summary>
    public sealed class ExampleLifetimeScope : LifetimeScope
    {
        [SerializeField] private ScreenCatalog screenCatalog;
        [SerializeField] private PanelSettings panelSettings;

        protected override void Configure(IContainerBuilder builder)
        {
            // Registers UIKitContext (disposed with this scope), IScreenNavigator and IViewFactory.
            builder.AddUIKit(panelSettings, screenCatalog);

            builder.Register<PlayerProfile>(Lifetime.Singleton);
            builder.Register<GameSettings>(Lifetime.Singleton);

            builder.RegisterScreen<MainMenuViewModel, MainMenuView>();
            builder.RegisterScreen<SettingsViewModel, SettingsView>();
            builder.RegisterScreen<ProfileViewModel, ProfileView>();
            // Child view of Profile: its parent supplies the ViewModel.
            builder.RegisterView<PlayerBadgeViewModel, PlayerBadgeView>();
            builder.RegisterScreen<ConfirmPopupViewModel, ConfirmPopupView>();
            builder.RegisterScreen<NameInputPopupViewModel, NameInputPopupView>();

            builder.RegisterEntryPoint<ExampleEntryPoint>();
        }
    }
}
