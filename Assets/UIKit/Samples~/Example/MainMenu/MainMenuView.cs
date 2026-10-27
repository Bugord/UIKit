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
            statusLabel = Require<Label>("status-label");

            BindText("greeting-label", nameof(MainMenuViewModel.Greeting));
            BindText(statusLabel, nameof(MainMenuViewModel.Status));

            Observe(nameof(MainMenuViewModel.Status), FlashStatus);

            // Each button is disabled while its command, or another on the same gate, runs.
            BindCommand("play-button", ViewModel.StartGame);
            BindCommand("settings-button", ViewModel.OpenSettings);
            BindCommand("rename-button", ViewModel.RenamePlayer);
            BindCommand("profile-button", ViewModel.OpenProfile);
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
