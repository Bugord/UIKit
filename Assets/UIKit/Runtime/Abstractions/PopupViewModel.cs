namespace UIKit.Runtime.Abstractions
{
    public abstract class PopupViewModel<TResult> : ResultViewModel<TResult>, ICancelable, IPopup
    {
        protected abstract TResult CancelResult { get; }

        public virtual void Cancel()
        {
            SetResult(CancelResult);
        }
    }
}