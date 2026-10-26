using UIKit;
using UnityEngine.UIElements;

namespace Example
{
    /// <summary>
    /// Keeps its labels in sync through data bindings, so it holds no label state of its own,
    /// and uses <c>Observe</c> for what a binding cannot express: a brief flash of the status
    /// label each time the status changes. Compare PlayerBadge.uxml for bindings in UXML.
    /// </summary>
    public class MainMenuView : View<MainMenuViewModel>
    {
        private const string FlashClass = "status-label--flash";
        private const long FlashMilliseconds = 1000;

        private Label statusLabel;
        private IVisualElementScheduledItem flashEnd;

        protected override void OnAttach()
        {
            statusLabel = Root.Q<Label>("status-label");

            BindProperty(Root.Q<Label>("greeting-label"), "text", nameof(MainMenuViewModel.Greeting));
            BindProperty(statusLabel, "text", nameof(MainMenuViewModel.Status));

            Observe(nameof(MainMenuViewModel.Status), FlashStatus);

            // Navigation started from a click is fire-and-forget: Forget() logs any failure.
            Root.Q<Button>("play-button").clicked += () => ViewModel.StartGameAsync().Forget();
            Root.Q<Button>("settings-button").clicked += () => ViewModel.OpenSettingsAsync().Forget();
            Root.Q<Button>("rename-button").clicked += () => ViewModel.RenamePlayerAsync().Forget();
            Root.Q<Button>("profile-button").clicked += () => ViewModel.OpenProfileAsync().Forget();
        }

        // A change during a flash restarts its timer rather than queueing a second one.
        private void FlashStatus()
        {
            statusLabel.AddToClassList(FlashClass);

            if (flashEnd == null) {
                flashEnd = statusLabel.schedule.Execute(() => statusLabel.RemoveFromClassList(FlashClass))
                    .StartingIn(FlashMilliseconds);
            } else {
                flashEnd.ExecuteLater(FlashMilliseconds);
            }
        }
    }
}
