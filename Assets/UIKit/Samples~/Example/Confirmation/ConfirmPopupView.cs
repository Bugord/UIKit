using UIKit;
using UnityEngine.UIElements;

namespace Example.Confirmation
{
    public class ConfirmPopupView : View<ConfirmPopupViewModel>
    {
        protected override void OnAttach()
        {
            Root.Q<Button>("yes-button").clicked += ViewModel.Confirm;
            Root.Q<Button>("no-button").clicked += ViewModel.Cancel;
        }
    }
}
