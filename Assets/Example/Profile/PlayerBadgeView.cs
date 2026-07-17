using UIKit.Runtime.Core;
using UnityEngine.UIElements;

namespace Example.Profile
{
    public class PlayerBadgeView : View<PlayerBadgeViewModel>
    {
        private Label scoreLabel;

        protected override void OnAttach()
        {
            Root.Q<Label>("level-label").text = $"Level {ViewModel.Level}";

            scoreLabel = Root.Q<Label>("score-label");
            scoreLabel.text = $"Score: {ViewModel.Score}";

            Root.Q<Button>("add-score-button").clicked += () => {
                ViewModel.AddScore(10);
                scoreLabel.text = $"Score: {ViewModel.Score}";
            };
        }
    }
}
