using UIKit;

namespace Example.Rename
{
    // A stray click outside the panel should not throw away a half-typed name, so only Escape
    // (or the Cancel button) dismisses it.
    [UIScreen("RenameName", Dismiss = DismissTriggers.NavigationCancel)]
    public class NameInputPopupViewModel : PopupViewModel<string>, IPayloadReceiver<string>
    {
        public string CurrentName { get; private set; } = string.Empty;

        /// <summary>
        /// <c>null</c> on cancel, so the caller can tell cancelling apart from confirming the
        /// same name.
        /// </summary>
        protected override string CancelResult => null;

        public void SetPayload(string payload) => CurrentName = payload ?? string.Empty;

        public void Confirm(string name) => SetResult(name?.Trim() ?? string.Empty);
    }
}
