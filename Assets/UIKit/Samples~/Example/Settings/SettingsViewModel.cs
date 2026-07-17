using Cysharp.Threading.Tasks;
using UIKit.Runtime.Abstractions;

namespace Example.Settings
{
    public class SettingsViewModel : ViewModel
    {
        private readonly IScreenNavigator navigator;

        public float Volume { get; private set; } = 0.5f;
        public bool Muted { get; private set; }

        public SettingsViewModel(IScreenNavigator navigator)
        {
            this.navigator = navigator;
        }

        public void SetVolume(float volume) => Volume = volume;

        public void SetMuted(bool muted) => Muted = muted;

        public UniTask CloseAsync() => navigator.CloseAsync(this);
    }
}
