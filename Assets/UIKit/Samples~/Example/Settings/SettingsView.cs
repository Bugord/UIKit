using UIKit;
using UnityEngine.UIElements;

namespace Example.Settings
{
    public class SettingsView : View<SettingsViewModel>
    {
        protected override void OnAttach()
        {
            BindProperty(Root.Q<Slider>("volume-slider"), "value", nameof(SettingsViewModel.Volume), BindingMode.TwoWay);
            BindProperty(Root.Q<Toggle>("muted-toggle"), "value", nameof(SettingsViewModel.Muted), BindingMode.TwoWay);
            BindProperty(Root.Q<Label>("volume-label"), "text", nameof(SettingsViewModel.VolumeText));

            Root.Q<Button>("back-button").clicked += () => ViewModel.CloseAsync().Forget();
        }
    }
}
