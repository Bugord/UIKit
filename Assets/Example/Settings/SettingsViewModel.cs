using UIKit;
using Unity.Properties;
using UnityEngine;

namespace Example.Settings
{
    public class SettingsViewModel : ViewModel
    {
        private readonly IScreenNavigator navigator;
        private readonly GameSettings settings;

        private float volume;
        private bool muted;

        // Bound two-way by SettingsView: the slider writes through this setter, and
        // SetProperty's change notification updates the bound VolumeText label.
        [CreateProperty]
        public float Volume
        {
            get => volume;
            set {
                if (SetProperty(ref volume, value)) {
                    settings.Volume = value;
                    Notify(nameof(VolumeText));
                }
            }
        }

        [CreateProperty]
        public bool Muted
        {
            get => muted;
            set {
                if (SetProperty(ref muted, value)) {
                    settings.Muted = value;
                    Notify(nameof(VolumeText));
                }
            }
        }

        [CreateProperty] public string VolumeText => muted ? "Muted" : $"{Mathf.RoundToInt(volume * 100)}%";

        public SettingsViewModel(IScreenNavigator navigator, GameSettings settings)
        {
            this.navigator = navigator;
            this.settings = settings;
            volume = settings.Volume;
            muted = settings.Muted;
        }

        public Awaitable CloseAsync() => navigator.CloseAsync(this);
    }
}
