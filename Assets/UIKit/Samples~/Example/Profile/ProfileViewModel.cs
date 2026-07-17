using Cysharp.Threading.Tasks;
using UIKit.Runtime.Abstractions;

namespace Example.Profile
{
    public class ProfileViewModel : ViewModel, IPayloadReceiver<string>
    {
        private readonly IScreenNavigator navigator;

        public string PlayerName { get; private set; } = string.Empty;
        public PlayerBadgeViewModel Badge { get; }

        public ProfileViewModel(IScreenNavigator navigator, PlayerBadgeViewModel badge)
        {
            this.navigator = navigator;
            Badge = badge;
        }

        public void SetPayload(string payload) => PlayerName = payload;

        public UniTask CloseAsync() => navigator.CloseAsync(this);
    }
}
