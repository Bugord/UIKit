using UIKit;
using UnityEngine;
using UnityEngine.UIElements;

namespace Example.Rename
{
    public class NameInputPopupView : View<NameInputPopupViewModel>
    {
        private TextField nameField;

        protected override void OnAttach()
        {
            // Set once and read back on OK, no binding: the field is the only place the draft
            // name lives until it is confirmed.
            nameField = Require<TextField>("name-field");
            nameField.value = ViewModel.CurrentName;
            // TrickleDown so Enter is seen before the text input handles it.
            nameField.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

            Require<Button>("ok-button").clicked += Confirm;
            Require<Button>("cancel-button").clicked += ViewModel.Cancel;
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter) {
                Confirm();
            }
        }

        private void Confirm() => ViewModel.Confirm(nameField.value);
    }
}
