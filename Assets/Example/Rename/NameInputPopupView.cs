using UIKit.Runtime.Core;
using UnityEngine.UIElements;

namespace Example.Rename
{
    public class NameInputPopupView : View<NameInputPopupViewModel>
    {
        protected override void OnAttach()
        {
            var nameField = Root.Q<TextField>("name-field");
            nameField.value = ViewModel.InitialName;

            Root.Q<Button>("ok-button").clicked += () => ViewModel.SetResult(nameField.value);
            Root.Q<Button>("cancel-button").clicked += ViewModel.Cancel;
        }
    }
}
