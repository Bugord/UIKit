using Example.Confirmation;
using Example.Profile;
using Example.Rename;
using Example.Settings;
using UIKit;
using UnityEngine;
using UnityEngine.UIElements;

namespace Example
{
    /// <summary>
    /// Runs the example without a DI container: creates the <see cref="UIKitContext"/>,
    /// registers each screen with factories that hand the ViewModels their dependencies, and
    /// opens the main menu.
    /// </summary>
    public sealed class ExampleBootstrap : MonoBehaviour
    {
        [SerializeField] private PanelSettings panelSettings;
        [SerializeField] private ScreenCatalog screenCatalog;

        private UIKitContext ui;

        private void Start()
        {
            // Plain objects the screens share; a DI container would register them as singletons.
            var profile = new PlayerProfile();
            var settings = new GameSettings();

            ui = new UIKitContext(panelSettings, screenCatalog);
            var navigator = ui.Navigator;

            // The factories run each time a screen opens, so every open gets a fresh ViewModel.
            ui.Register<MainMenuViewModel, MainMenuView>(() => new MainMenuViewModel(navigator, profile))
                .Register<SettingsViewModel, SettingsView>(() => new SettingsViewModel(navigator, settings))
                .Register<ProfileViewModel, ProfileView>(() => new ProfileViewModel(navigator, profile))
                // Only ever created as a child view of Profile, which supplies its ViewModel.
                .RegisterView<PlayerBadgeViewModel, PlayerBadgeView>()
                .Register<ConfirmPopupViewModel, ConfirmPopupView>(() => new ConfirmPopupViewModel())
                .Register<NameInputPopupViewModel, NameInputPopupView>(() => new NameInputPopupViewModel());

            navigator.OpenAsync<MainMenuViewModel>(destroyCancellationToken).Forget();
        }

        private void OnDestroy()
        {
            // Closes every open screen, disposes their ViewModels and destroys the panel.
            ui?.Dispose();
            ui = null;
        }
    }
}
