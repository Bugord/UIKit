using Cysharp.Threading.Tasks;
using Example.Confirmation;
using Example.Profile;
using Example.Rename;
using Example.Settings;
using UIKit.Runtime.Abstractions;

namespace Example
{
    public class MainMenuViewModel : ViewModel
    {
        private readonly IScreenNavigator navigator;

        private ScreenHandle<SettingsViewModel> settingsHandle;
        private ScreenHandle<ProfileViewModel> profileHandle;

        public string PlayerName { get; private set; } = "Player";

        public MainMenuViewModel(IScreenNavigator navigator)
        {
            this.navigator = navigator;
        }

        public UniTask<bool> StartGameAsync() => navigator.RunAsync<ConfirmPopupViewModel, bool>();

        public async UniTask OpenSettingsAsync()
        {
            if (settingsHandle is { IsOpen: true }) {
                return;
            }

            settingsHandle = await navigator.OpenAsync<SettingsViewModel>();
        }

        public async UniTask<string> RenamePlayerAsync()
        {
            PlayerName = await navigator.RunAsync<NameInputPopupViewModel, string, string>(PlayerName);
            return PlayerName;
        }

        public async UniTask OpenProfileAsync()
        {
            if (profileHandle is { IsOpen: true }) {
                return;
            }

            profileHandle = await navigator.OpenAsync<ProfileViewModel, string>(PlayerName);
        }
    }
}
