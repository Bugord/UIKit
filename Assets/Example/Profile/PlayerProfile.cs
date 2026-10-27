using System;

namespace Example.Profile
{
    /// <summary>
    /// The player's state, shared by every screen that shows it. It outlives the screens, so
    /// ViewModels that subscribe to <see cref="Changed"/> must unsubscribe when disposed.
    /// </summary>
    public sealed class PlayerProfile
    {
        private const int ScorePerLevel = 50;

        private string name = "Player";

        public event Action Changed;

        public string Name
        {
            get => name;
            set {
                if (name == value) {
                    return;
                }

                name = value;
                Changed?.Invoke();
            }
        }

        public int Score { get; private set; }

        public int Level => 1 + Score / ScorePerLevel;

        public void AddScore(int amount)
        {
            Score += amount;
            Changed?.Invoke();
        }
    }
}
