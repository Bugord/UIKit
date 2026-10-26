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
        // SetProperty's change notification also notifies VolumeText, which depends on it.
        [CreateProperty]
        public float Volume
        {
            get => volume;
            set {
                if (SetProperty(ref volume, value)) {
                    settings.Volume = value;
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
                }
            }
        }

        [CreateProperty, DependsOn(nameof(Volume), nameof(Muted))]
        public string VolumeText => muted ? "Muted" : $"{Mathf.RoundToInt(volume * 100)}%";

        public AsyncCommand Close { get; }

        public SettingsViewModel(IScreenNavigator navigator, GameSettings settings)
        {
            this.navigator = navigator;
            this.settings = settings;
            volume = settings.Volume;
            muted = settings.Muted;
            Close = new AsyncCommand(() => navigator.CloseAsync(this));
        }
    }
}
