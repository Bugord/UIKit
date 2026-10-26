using UIKit;
using UnityEngine.UIElements;

namespace Example
{
    /// <summary>
    /// Updates its labels by hand from <see cref="OnPropertyChanged"/> - the alternative to
    /// data binding for anything a binding cannot express. Compare PlayerBadge.uxml.
    /// </summary>
    public class MainMenuView : View<MainMenuViewModel>
    {
        private Label greetingLabel;
        private Label statusLabel;

        protected override void OnAttach()
        {
            greetingLabel = Root.Q<Label>("greeting-label");
            statusLabel = Root.Q<Label>("status-label");

            greetingLabel.text = ViewModel.Greeting;
            statusLabel.text = ViewModel.Status;

            // Navigation started from a click is fire-and-forget: Forget() logs any failure.
            Root.Q<Button>("play-button").clicked += () => ViewModel.StartGameAsync().Forget();
            Root.Q<Button>("settings-button").clicked += () => ViewModel.OpenSettingsAsync().Forget();
            Root.Q<Button>("rename-button").clicked += () => ViewModel.RenamePlayerAsync().Forget();
            Root.Q<Button>("profile-button").clicked += () => ViewModel.OpenProfileAsync().Forget();
        }

        protected override void OnPropertyChanged(string propertyName)
        {
            switch (propertyName) {
                case nameof(MainMenuViewModel.Greeting):
                    greetingLabel.text = ViewModel.Greeting;
                    break;
                case nameof(MainMenuViewModel.Status):
                    statusLabel.text = ViewModel.Status;
                    break;
            }
        }
    }
}
