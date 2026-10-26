using System;
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
        private bool navigating;

        [CreateProperty]
        public string Greeting => $"Hello, {profile.Name}";

        [CreateProperty]
        public string Status
        {
            get => status;
            private set => SetProperty(ref status, value);
        }

        public MainMenuViewModel(IScreenNavigator navigator, PlayerProfile profile)
        {
            this.navigator = navigator;
            this.profile = profile;
            profile.Changed += OnProfileChanged;
        }

        // Each call passes LifetimeToken: if the menu is disposed while a popup is open, the
        // wait is cancelled instead of resuming into a dead ViewModel.

        public Awaitable StartGameAsync() => NavigateAsync(async () => {
            var confirmed = await navigator.RunAsync<ConfirmPopupViewModel, bool>(LifetimeToken);
            Status = confirmed ? "Game started!" : "Start cancelled.";
        });

        public Awaitable RenamePlayerAsync() => NavigateAsync(async () => {
            var name = await navigator.RunAsync<NameInputPopupViewModel, string, string>(profile.Name, LifetimeToken);

            if (name == null) {
                Status = "Rename cancelled.";
            } else if (name.Length == 0 || name == profile.Name) {
                Status = "Name unchanged.";
            } else {
                profile.Name = name;
                Status = $"Player renamed to '{name}'.";
            }
        });

        public Awaitable OpenSettingsAsync() => NavigateAsync(async () => {
            await navigator.OpenAsync<SettingsViewModel>(LifetimeToken);
        });

        public Awaitable OpenProfileAsync() => NavigateAsync(async () => {
            await navigator.OpenAsync<ProfileViewModel>(LifetimeToken);
        });

        protected override void OnDispose()
        {
            profile.Changed -= OnProfileChanged;
        }

        // Ignores a click while the previous one is still being handled, so a double click
        // cannot queue the same screen or popup twice.
        private async Awaitable NavigateAsync(Func<Awaitable> navigate)
        {
            if (navigating) {
                return;
            }

            navigating = true;

            try {
                await navigate();
            } finally {
                navigating = false;
            }
        }

        private void OnProfileChanged() => Notify(nameof(Greeting));
    }
}
