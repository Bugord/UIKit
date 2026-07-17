using UIKit.Runtime.Abstractions;

namespace Example.Profile
{
    public class PlayerBadgeViewModel : ViewModel
    {
        public int Level { get; private set; } = 1;
        public int Score { get; private set; }

        public void AddScore(int amount) => Score += amount;
    }
}
