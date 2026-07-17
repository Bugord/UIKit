using Cysharp.Threading.Tasks;
using UIKit.Runtime.Abstractions;

namespace Example.Profile
{
    public class ProfileViewModel : ViewModel, IPayloadReceiver<string>
    {
        private readonly IScreenNavigator navigator;

        public string PlayerName { get; private set; } = string.Empty;

        public ProfileViewModel(IScreenNavigator navigator)
        {
            this.navigator = navigator;
        }

        public void SetPayload(string payload) => PlayerName = payload;

        public UniTask CloseAsync() => navigator.CloseAsync(this);
    }
}
