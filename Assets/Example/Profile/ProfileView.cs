using Cysharp.Threading.Tasks;
using UIKit.Runtime.Core;
using UnityEngine.UIElements;

namespace Example.Profile
{
    public class ProfileView : View<ProfileViewModel>
    {
        protected override void OnAttach()
        {
            Root.Q<Label>("name-label").text = $"Welcome, {ViewModel.PlayerName}!";
            Root.Q<Button>("back-button").clicked += () => ViewModel.CloseAsync().Forget();
        }
    }
}
