using UIKit;

namespace Example.Profile
{
    public class PlayerBadgeView : View<PlayerBadgeViewModel>
    {
        protected override void OnAttach()
        {
            // The labels are bound in PlayerBadge.uxml; only the button needs code. The badge
            // ViewModel outlives this view, so the binding ends when the view is disposed.
            BindCommand("add-score-button", ViewModel.AddScore);
        }
    }
}
