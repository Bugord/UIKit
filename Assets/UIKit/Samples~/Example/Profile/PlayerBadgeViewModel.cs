using UIKit;
using Unity.Properties;

namespace Example.Profile
{
    /// <summary>
    /// Shows the player's profile. Its view has no update code at all: PlayerBadge.uxml binds
    /// the labels to the <c>[CreateProperty]</c> members below, and <see cref="ViewModel.Notify"/>
    /// tells the bindings when to refresh.
    /// </summary>
    public class PlayerBadgeViewModel : ViewModel
    {
        private readonly PlayerProfile profile;

        public PlayerBadgeViewModel(PlayerProfile profile)
        {
            this.profile = profile;
            profile.Changed += OnProfileChanged;
        }

        // Strings rather than ints so the labels bind without a converter.
        [CreateProperty] public string PlayerName => profile.Name;
        [CreateProperty] public string LevelText => $"Level {profile.Level}";
        [CreateProperty] public string ScoreText => $"Score: {profile.Score}";

        public void AddScore(int amount) => profile.AddScore(amount);

        protected override void OnDispose()
        {
            // The profile outlives this badge; without this it would keep the badge alive.
            profile.Changed -= OnProfileChanged;
        }

        private void OnProfileChanged()
        {
            // Computed properties have no setter to raise the change, so notify each one.
            Notify(nameof(PlayerName));
            Notify(nameof(LevelText));
            Notify(nameof(ScoreText));
        }
    }
}
