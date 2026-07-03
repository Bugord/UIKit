using System.Threading;
using Cysharp.Threading.Tasks;

namespace UI.Navigation
{
    public class ResultViewModel<TResult> : ViewModel
    {
        protected readonly UniTaskCompletionSource<TResult> ResultSource = new();
        protected CancellationToken ResultCancellationToken;

        public virtual async UniTask<TResult> WaitForResultAsync(CancellationToken token = default)
        {
            await using var registration = token.Register(() => ResultSource.TrySetCanceled());
            ResultCancellationToken = token;
            return await ResultSource.Task;
        }
        
        protected void SetResult(TResult result) => ResultSource.TrySetResult(result);

        public override void Dispose()
        {
            ResultSource.TrySetCanceled();
            base.Dispose();
        }
    }
}