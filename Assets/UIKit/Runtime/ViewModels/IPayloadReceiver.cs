namespace UIKit
{
    /// <summary>
    /// Implemented by a ViewModel that takes input when opened. The navigator calls
    /// <see cref="SetPayload"/> before the view is created, so the payload is already there
    /// in <see cref="View{TViewModel}.OnAttach"/>.
    /// </summary>
    public interface IPayloadReceiver<in TPayload>
    {
        void SetPayload(TPayload payload);
    }
}
