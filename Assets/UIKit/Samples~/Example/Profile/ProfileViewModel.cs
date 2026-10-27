using UIKit;
using Unity.Properties;

namespace Example.Profile
{
    public class ProfileViewModel : ViewModel
    {
        private readonly IScreenNavigator navigator;
        private readonly PlayerProfile profile;

        /// <summary>
        /// Owned by this ViewModel, so the view creates the badge view without letting it
        /// dispose the badge.
        /// </summary>
        public PlayerBadgeViewModel Badge { get; }

        // The name cannot change while this screen is open, so it never needs a Notify.
        [CreateProperty] public string Title => $"Welcome, {profile.Name}!";

        public AsyncCommand Close { get; }

        public ProfileViewModel(IScreenNavigator navigator, PlayerProfile profile)
        {
            this.navigator = navigator;
            this.profile = profile;
            Badge = new PlayerBadgeViewModel(profile);
            Close = new AsyncCommand(() => navigator.CloseAsync(this));
        }

        protected override void OnDispose()
        {
            Badge.Dispose();
        }
    }
}
