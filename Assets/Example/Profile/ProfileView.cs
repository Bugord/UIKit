using UIKit;
using UnityEngine.UIElements;

namespace Example.Profile
{
    public class ProfileView : View<ProfileViewModel>
    {
        protected override void OnAttach()
        {
            // The code equivalent of a UXML binding; PlayerBadge.uxml shows the UXML form.
            BindText("name-label", nameof(ProfileViewModel.Title));

            BindCommand("back-button", ViewModel.Close);

            // Created through this view's ViewFactory, so it is disposed together with this
            // view. The badge's root gets the badge ViewModel as its own dataSource, which is
            // what its UXML bindings resolve against.
            ViewFactory.Create<PlayerBadgeView, PlayerBadgeViewModel>(
                ViewModel.Badge, Require<VisualElement>("badge-container"), disposeViewModel: false);
        }
    }
}
