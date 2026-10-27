using Example.Confirmation;
using Example.Profile;
using Example.Rename;
using Example.Settings;
using UIKit;
using UIKit.Zenject;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

namespace Example.Zenject
{
    /// <summary>
    /// The same example as ExampleBootstrap, wired with Zenject. Listed in the scene's
    /// SceneContext under Mono Installers.
    /// </summary>
    public sealed class ExampleInstaller : MonoInstaller
    {
        [SerializeField] private ScreenCatalog screenCatalog;
        [SerializeField] private PanelSettings panelSettings;

        public override void InstallBindings()
        {
            // Binds UIKitContext (disposed with the container), IScreenNavigator and IViewFactory.
            Container.BindUIKit(panelSettings, screenCatalog);

            Container.Bind<PlayerProfile>().AsSingle();
            Container.Bind<GameSettings>().AsSingle();

            Container.RegisterScreen<MainMenuViewModel, MainMenuView>();
            Container.RegisterScreen<SettingsViewModel, SettingsView>();
            Container.RegisterScreen<ProfileViewModel, ProfileView>();
            // Child view of Profile: its parent supplies the ViewModel.
            Container.RegisterView<PlayerBadgeViewModel, PlayerBadgeView>();
            Container.RegisterScreen<ConfirmPopupViewModel, ConfirmPopupView>();
            Container.RegisterScreen<NameInputPopupViewModel, NameInputPopupView>();

            Container.BindInterfacesTo<ExampleEntryPoint>().AsSingle();
        }
    }
}
