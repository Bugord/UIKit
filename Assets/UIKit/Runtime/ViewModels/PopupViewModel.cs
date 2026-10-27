namespace UIKit
{
    /// <summary>
    /// A result-producing popup. Dismissing it - backdrop click, Escape / gamepad cancel,
    /// <see cref="IScreenNavigator.GoBackAsync"/> or a close from elsewhere - resolves it with
    /// <see cref="CancelResult"/>.
    /// </summary>
    public abstract class PopupViewModel<TResult> : ResultViewModel<TResult>, ICancelable, IPopup
    {
        protected abstract TResult CancelResult { get; }

        public virtual void Cancel()
        {
            SetResult(CancelResult);
        }
    }
}
