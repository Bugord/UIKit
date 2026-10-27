using UIKit;
using Unity.Properties;

namespace Example.Profile
{
    /// <summary>
    /// Shows the player's profile. Its view has no update code at all: PlayerBadge.uxml binds
    /// the labels to the <c>[CreateProperty]</c> members below, and <c>Notify</c> tells the
    /// bindings when to refresh.
    /// </summary>
    public class PlayerBadgeViewModel : ViewModel
    {
        private readonly PlayerProfile profile;

        // Strings rather than ints so the labels bind without a converter.
        [CreateProperty] public string PlayerName => profile.Name;
        [CreateProperty] public string LevelText => $"Level {profile.Level}";
        [CreateProperty] public string ScoreText => $"Score: {profile.Score}";

        public Command AddScore { get; }

        public PlayerBadgeViewModel(PlayerProfile profile)
        {
            this.profile = profile;
            profile.Changed += OnProfileChanged;
            AddScore = new Command(() => profile.AddScore(10));
        }

        protected override void OnDispose()
        {
            // The profile outlives this badge; without this it would keep the badge alive.
            profile.Changed -= OnProfileChanged;
        }

        // The state lives in PlayerProfile, outside this ViewModel, so there is no source
        // property for [DependsOn] to follow: notify every property computed from it at once.
        private void OnProfileChanged() =>
            Notify(nameof(PlayerName), nameof(LevelText), nameof(ScoreText));
    }
}
