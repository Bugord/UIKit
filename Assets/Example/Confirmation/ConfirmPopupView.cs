using UIKit;
using UnityEngine.UIElements;

namespace Example.Confirmation
{
    public class ConfirmPopupView : View<ConfirmPopupViewModel>
    {
        protected override void OnAttach()
        {
            // Plain handlers rather than commands: the popup resolves once, and the ViewModel and
            // its buttons go away together, so there is no state to follow and nothing to unbind.
            Require<Button>("yes-button").clicked += ViewModel.Confirm;
            Require<Button>("no-button").clicked += ViewModel.Cancel;
        }
    }
}
