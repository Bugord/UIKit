using UIKit;

namespace Example.Confirmation
{
    // The UXML is Confirmation.uxml, so the id is given rather than derived ("ConfirmPopup").
    [UIScreen("Confirmation")]
    public class ConfirmPopupViewModel : PopupViewModel<bool>
    {
        // What a backdrop click, Escape or GoBackAsync resolves the popup with.
        protected override bool CancelResult => false;

        public void Confirm() => SetResult(true);
    }
}
