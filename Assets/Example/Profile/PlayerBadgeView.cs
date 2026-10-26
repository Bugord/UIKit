using UIKit;
using UnityEngine.UIElements;

namespace Example.Profile
{
    public class PlayerBadgeView : View<PlayerBadgeViewModel>
    {
        protected override void OnAttach()
        {
            // The labels are bound in PlayerBadge.uxml; only the button needs code.
            Root.Q<Button>("add-score-button").clicked += () => ViewModel.AddScore(10);
        }
    }
}
