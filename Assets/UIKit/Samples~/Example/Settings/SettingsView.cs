using Cysharp.Threading.Tasks;
using UIKit.Runtime.Core;
using UnityEngine.UIElements;

namespace Example.Settings
{
    public class SettingsView : View<SettingsViewModel>
    {
        protected override void OnAttach()
        {
            var volumeSlider = Root.Q<Slider>("volume-slider");
            volumeSlider.value = ViewModel.Volume;
            volumeSlider.RegisterValueChangedCallback(evt => ViewModel.SetVolume(evt.newValue));

            var mutedToggle = Root.Q<Toggle>("muted-toggle");
            mutedToggle.value = ViewModel.Muted;
            mutedToggle.RegisterValueChangedCallback(evt => ViewModel.SetMuted(evt.newValue));

            Root.Q<Button>("back-button").clicked += () => ViewModel.CloseAsync().Forget();
        }
    }
}
