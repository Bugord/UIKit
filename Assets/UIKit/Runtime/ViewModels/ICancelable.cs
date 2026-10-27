namespace UIKit
{
    /// <summary>
    /// Implemented by a ViewModel that can resolve itself as cancelled. The navigator calls
    /// <see cref="Cancel"/> before closing it from outside, so a pending
    /// <see cref="IScreenNavigator.RunAsync{TViewModel,TResult}"/> returns a result instead of
    /// throwing.
    /// </summary>
    public interface ICancelable
    {
        void Cancel();
    }
}
