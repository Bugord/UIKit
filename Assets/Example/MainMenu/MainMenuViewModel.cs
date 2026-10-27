using Example.Confirmation;
using Example.Profile;
using Example.Rename;
using Example.Settings;
using UIKit;
using Unity.Properties;
using UnityEngine;

namespace Example
{
    public class MainMenuViewModel : ViewModel
    {
        private readonly IScreenNavigator navigator;
        private readonly PlayerProfile profile;

        private string status = string.Empty;

        [CreateProperty]
        public string Greeting => $"Hello, {profile.Name}";

        [CreateProperty]
        public string Status
        {
            get => status;
            private set => SetProperty(ref status, value);
        }

        // One gate for all four: while one of them navigates, none of the others can start, so
        // two buttons clicked in the same frame cannot open two screens. The view disables the
        // buttons meanwhile.
        public AsyncCommand StartGame { get; }
        public AsyncCommand RenamePlayer { get; }
        public AsyncCommand OpenSettings { get; }
        public AsyncCommand OpenProfile { get; }

        public MainMenuViewModel(IScreenNavigator navigator, PlayerProfile profile)
        {
            this.navigator = navigator;
            this.profile = profile;
            profile.Changed += OnProfileChanged;

            var navigation = new CommandGate();
            StartGame = new AsyncCommand(StartGameAsync, navigation);
            RenamePlayer = new AsyncCommand(RenamePlayerAsync, navigation);
            OpenSettings = new AsyncCommand(OpenSettingsAsync, navigation);
            OpenProfile = new AsyncCommand(OpenProfileAsync, navigation);
        }

        protected override void OnDispose()
        {
            profile.Changed -= OnProfileChanged;
        }

        // Each call passes LifetimeToken: if the menu is disposed while a popup is open, the
        // wait is cancelled instead of resuming into a dead ViewModel.

        private async Awaitable StartGameAsync()
        {
            var confirmed = await navigator.RunAsync<ConfirmPopupViewModel, bool>(LifetimeToken);
            Status = confirmed ? "Game started!" : "Start cancelled.";
        }

        private async Awaitable RenamePlayerAsync()
        {
            var name = await navigator.RunAsync<NameInputPopupViewModel, string, string>(profile.Name, LifetimeToken);

            if (name == null) {
                Status = "Rename cancelled.";
            } else if (name.Length == 0 || name == profile.Name) {
                Status = "Name unchanged.";
            } else {
                profile.Name = name;
                Status = $"Player renamed to '{name}'.";
            }
        }

        private async Awaitable OpenSettingsAsync()
        {
            await navigator.OpenAsync<SettingsViewModel>(LifetimeToken);
        }

        private async Awaitable OpenProfileAsync()
        {
            await navigator.OpenAsync<ProfileViewModel>(LifetimeToken);
        }

        private void OnProfileChanged() => Notify(nameof(Greeting));
    }
}
