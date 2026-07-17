using UIKit.Runtime.Abstractions;
using UIKit.Runtime.Core;
using UIKit.Runtime.Utils;

namespace Example.Rename
{
    [UIScreen(UILayer.Popup, "RenameName")]
    public class NameInputPopupViewModel : PopupViewModel<string>, IPayloadReceiver<string>
    {
        public string InitialName { get; private set; } = string.Empty;

        protected override string CancelResult => InitialName;

        public void SetPayload(string payload) => InitialName = payload;
    }
}
