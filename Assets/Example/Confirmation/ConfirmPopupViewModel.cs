using UIKit.Runtime.Abstractions;
using UIKit.Runtime.Utils;

namespace Example.Confirmation
{
    [UIScreen("Confirmation")]
    public class ConfirmPopupViewModel : PopupViewModel<bool>
    {
        protected override bool CancelResult => false;
    }
}
