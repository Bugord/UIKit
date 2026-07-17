using Cysharp.Threading.Tasks;
using UIKit.Runtime.Core;
using UnityEngine.UIElements;

namespace Example.Profile
{
    public class ProfileView : View<ProfileViewModel>
    {
        private PlayerBadgeView badgeView;

        protected override void OnAttach()
        {
            Root.Q<Label>("name-label").text = $"Welcome, {ViewModel.PlayerName}!";
            Root.Q<Button>("back-button").clicked += () => ViewModel.CloseAsync().Forget();

            var badgeContainer = Root.Q<VisualElement>("badge-container");
            badgeView = ViewFactory.Create<PlayerBadgeView, PlayerBadgeViewModel>(ViewModel.Badge, badgeContainer);
        }

        public override void Dispose()
        {
            badgeView.Dispose();
            base.Dispose();
        }
    }
}
