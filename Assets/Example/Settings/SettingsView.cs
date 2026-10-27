using UIKit;

namespace Example.Settings
{
    public class SettingsView : View<SettingsViewModel>
    {
        protected override void OnAttach()
        {
            // Two-way by default: the slider and the toggle write back through the setters.
            BindValue("volume-slider", nameof(SettingsViewModel.Volume));
            BindValue("muted-toggle", nameof(SettingsViewModel.Muted));
            BindText("volume-label", nameof(SettingsViewModel.VolumeText));

            BindCommand("back-button", ViewModel.Close);
        }
    }
}
