namespace UIKit.Runtime.Abstractions
{
    public interface IPayloadReceiver<in TPayload>
    {
        void SetPayload(TPayload payload);
    }
}