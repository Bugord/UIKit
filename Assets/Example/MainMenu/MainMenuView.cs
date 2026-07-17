using Cysharp.Threading.Tasks;
using UIKit.Runtime.Core;
using UnityEngine.UIElements;

namespace Example
{
    public class MainMenuView : View<MainMenuViewModel>
    {
        private Label statusLabel;

        protected override void OnAttach()
        {
            statusLabel = Root.Q<Label>("status-label");

            Root.Q<Button>("play-button").clicked += () => PlayAsync().Forget();
            Root.Q<Button>("settings-button").clicked += () => ViewModel.OpenSettingsAsync().Forget();
            Root.Q<Button>("rename-button").clicked += () => RenameAsync().Forget();
            Root.Q<Button>("profile-button").clicked += () => ViewModel.OpenProfileAsync().Forget();
        }

        private async UniTaskVoid PlayAsync()
        {
            var confirmed = await ViewModel.StartGameAsync();
            statusLabel.text = confirmed ? "Game started!" : "Start cancelled.";
        }

        private async UniTaskVoid RenameAsync()
        {
            var name = await ViewModel.RenamePlayerAsync();
            statusLabel.text = $"Player renamed to '{name}'.";
        }
    }
}
