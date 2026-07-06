using System.Threading;
using Cysharp.Threading.Tasks;

namespace UIKit.Runtime.Abstractions
{
    public class ResultViewModel<TResult> : ViewModel, IResultViewModel<TResult>
    {
        protected readonly UniTaskCompletionSource<TResult> ResultSource = new();

        public virtual async UniTask<TResult> WaitForResultAsync(CancellationToken token = default)
        {
            await using var registration = token.Register(() => ResultSource.TrySetCanceled());
            return await ResultSource.Task;
        }

        public void SetResult(TResult result) => ResultSource.TrySetResult(result);

        public override void Dispose()
        {
            ResultSource.TrySetCanceled();
            base.Dispose();
        }
    }
}